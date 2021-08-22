using Sels.Core.Components.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sels.Core.Extensions;
using Sels.Core.Components.Logging;
using Sels.Core.Extensions.Linq;
using System.Linq;
using Sels.Core.Components.Parameters;
using Microsoft.Extensions.Logging;
using Sels.Core.Templates.FileSystem;

namespace Sels.Crypto.Chia.PlotBot.Models
{
    public class Plotter : IDisposable
    {
        // Fields
        private readonly IPlottingService _plottingService;
        private readonly List<PlottingInstance> _plottingInstances = new List<PlottingInstance>();

        // Properties
        #region PlotSettings
        /// <summary>
        /// Public pool key.
        /// </summary>
        public string PoolKey { get; set; }
        /// <summary>
        /// Pool contract address.
        /// </summary>
        public string PoolContractAddress { get; set; }
        /// <summary>
        /// Public farmer key.
        /// </summary>
        public string FarmerKey { get; set; }
        #endregion

        #region PlotterSettings
        /// <summary>
        /// Boolean indicating if this plotter is allowed to start new instances.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// How many hours before a plotting instance is considered timed out. This will cause Plot Bot to dispose the instance and raise an error.
        /// </summary>
        public int Timeout { get; set; }
        /// <summary>
        /// Unique name to identifiy this plotter config.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Size of plots that this plotter will create.
        /// </summary>
        public PlotSize PlotSize { get; set; }
        /// <summary>
        /// How many plots the instance will create.
        /// </summary>
        public int PlotAmount { get; set; } = 1;
        /// <summary>
        /// How many instances this plotter can use.
        /// </summary>
        public int MaxInstances { get; set; }
        /// <summary>
        /// Total amount of thread this plotter can use. Threads are divided between all instances.
        /// </summary>
        public int TotalThreads { get; set; }
        /// <summary>
        /// Total amount fo ram this plotter can use. Ram is divided between all instances.
        /// </summary>
        public int TotalRam { get; set; }
        /// <summary>
        /// How many buckets this plotter uses to plot.
        /// </summary>
        public int Buckets { get; set; }
        /// <summary>
        /// Command that starts a new process that creates plots.
        /// </summary>
        public string PlotCommand { get; set; }

        /// <summary>
        /// Component that extracts information from the progress file.
        /// </summary>
        public IPlotProgressParser PlotProgressParser { get; set; }

        /// <summary>
        /// Checks if plotter is allowed to plot to a certain drive
        /// </summary>
        public IPlotterDelayer[] PlotterDelayers { get; set; }
        #endregion

        #region PlotterWorkingSettings
        /// <summary>
        /// List of cache directories the plotter can use to create plots
        /// </summary>
        public CrossPlatformDirectory[] Caches { get; set; }
        /// <summary>
        /// Directory the plotter uses as working directory. This is where the progress files are placed to monitor the progress of the plotter.
        /// </summary>
        public CrossPlatformDirectory WorkingDirectory { get; set; }
        /// <summary>
        /// If we should archive progress files once a plot instance is done plotting. Can be handy to keep a history.
        /// </summary>
        public bool ArchiveProgressFiles { get; set; }
        #endregion

        #region State
        /// <summary>
        /// If this plotter can start new plotting instances.
        /// </summary>
        public bool CanPlotNew => Enabled && !TaggedForDeletion && _plottingInstances.Count < MaxInstances;
        /// <summary>
        /// Active instances that are plotting.
        /// </summary>
        public PlottingInstance[] Instances => _plottingInstances.ToArray();
        /// <summary>
        /// Indicates if this plotter has any active running instances.
        /// </summary>
        public bool HasRunningInstances => _plottingInstances.HasValue();
        /// <summary>
        /// Plotters tagged for deletion can't start new instances.
        /// </summary>
        public bool TaggedForDeletion { get; set; }
        #endregion

        public Plotter(IPlottingService plottingService)
        {
            _plottingService = plottingService.ValidateArgument(nameof(plottingService));
        }

        /// <summary>
        /// Checks if this plotter can plot to <paramref name="drive"/>.
        /// </summary>
        /// <param name="drive">Drive to check free space on</param>
        /// <returns></returns>
        public bool CanPlotToDrive(Drive drive)
        {
            using var logger = LoggingServices.TraceMethod(this);
            drive.ValidateArgument(nameof(drive));

            var canPlot = CanPlotNew && PlotterDelayers.All(x => x.CanStartInstance(this, drive)) && drive.CanBePlotted(PlotSize.FinalSize);

            LoggingServices.Debug($"Plotter {Alias} {(canPlot ? "can" : "can't")} plot to Drive {drive.Alias}");

            return canPlot;
        }

        /// <summary>
        /// Starts up a new instance that will create a plot for <paramref name="drive"/>.
        /// </summary>
        /// <param name="drive">Destination drive for the created plot</param>
        /// <returns>Plotting instance that is creating a new plot for <paramref name="drive"/></returns>
        public PlottingInstance PlotToDrive(Drive drive)
        {
            using var logger = LoggingServices.TraceMethod(this);
            drive.ValidateArgument(nameof(drive));

            LoggingServices.Debug($"{Alias} dividing resources to plot to {drive.Alias}");
            string plotCommand;
            var threads = TotalThreads / MaxInstances;
            var ram = TotalRam / MaxInstances;

            // Last instance takes the rest of the division
            if(_plottingInstances.Count == MaxInstances - 1)
            {
                threads = TotalThreads - (threads * _plottingInstances.Count);
                ram = TotalRam - (ram * _plottingInstances.Count);
            }

            var followNumber = 1;
            while(_plottingInstances.Any(x => x.FollowNumber.Equals(followNumber)))
            {
                followNumber++;
            }

            LoggingServices.Log($"New plotting instance will use {threads} Threads, {ram}MB Ram, {Buckets} buckets to create {PlotSize.Name} plots of size {PlotSize.FinalSize} using a cache size of {PlotSize.CreationSize} for destination drive {drive.Alias}");

            LoggingServices.Debug($"{Alias} preparing parameters to plot to {drive.Alias}");
            // Prepare parameters
            var parameterizer = new Parameterizer(false);

            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.MadMaxCommand, PlotBotConstants.Settings.MadMaxCommand);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.ChiaCommand, PlotBotConstants.Settings.ChiaCommand);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.PlotterAlias, Alias);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.PlotterInstance, followNumber);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.DriveAlias, drive.Alias);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.PlotSize, PlotSize.Name);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.PlotAmount, PlotAmount);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.Threads, threads);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.Buckets, Buckets);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.Ram, ram);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.Destination, drive.Directory.Source.FullName);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.PoolKey, PoolKey);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.PoolContractAddress, PoolContractAddress);
            parameterizer.AddParameter(PlotBotConstants.Parameters.Names.FarmerKey, FarmerKey);

            LoggingServices.Debug($"{Alias} creating cache directories to plot to {drive.Alias}");
            var caches = new List<DirectoryInfo>();
            var instanceSubDirectoryName = Guid.NewGuid().ToString();
            for (int i = 0; i < Caches.Length; i++)
            {
                var cacheDirectory = Caches[i].Source;
                var instanceDirectory = cacheDirectory.CreateSubdirectory(instanceSubDirectoryName);
                caches.Add(instanceDirectory);
                parameterizer.AddParameter($"{PlotBotConstants.Parameters.Names.Cache}_{i+1}", instanceDirectory);
            }

            // Generate command
            using (LoggingServices.TraceAction(LogLevel.Debug, "Applying command parameters"))
            {
                plotCommand = parameterizer.Apply(PlotCommand);
            }

            LoggingServices.Debug($"{Alias} creating instance to plot to {drive.Alias}");
            var instance = new PlottingInstance(followNumber, _plottingService, PlotProgressParser, plotCommand, Timeout, this, drive, PlotSize.FinalSize, x => {
                // Delete cache directories
                caches.ForceExecute(x => x.Delete(true), (x, ex) => LoggingServices.Log($"Could not delete cache directory {x.FullName} for plotting instance {x.Name}", ex));
                // Remove running instance
                RemoveInstance(x);
                drive.RemoveInstance(x);
            });

            // Register running instance
            RegisterInstance(instance);
            drive.RegisterInstance(instance);

            LoggingServices.Debug($"Plotter {Alias} has created an new instance with name {instance.Name} that will plot to {drive.Alias}");
            return instance;
        }

        private void RegisterInstance(PlottingInstance plottingInstance)
        {
            using var logger = LoggingServices.TraceMethod(this);
            plottingInstance.ValidateArgument(nameof(plottingInstance));

            LoggingServices.Debug($"Adding instance {plottingInstance} to Plotter {Alias}");

            _plottingInstances.Add(plottingInstance);
        }

        private void RemoveInstance(PlottingInstance plottingInstance)
        {
            using var logger = LoggingServices.TraceMethod(this);
            plottingInstance.ValidateArgument(nameof(plottingInstance));

            LoggingServices.Debug($"Removing instance {plottingInstance} from Plotter {Alias}");

            _plottingInstances.Remove(plottingInstance);
        }

        public void Dispose()
        {
            using var logger = LoggingServices.TraceMethod(this);
            // Stop all current instances
            Instances.ForceExecute(x => x.Dispose(), (x, ex) => LoggingServices.Log($"Error occured while disposing plotting instance {x.Name}", ex));
        }
    }
}
