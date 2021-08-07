using Sels.Crypto.Chia.PlotBot.Models.Config;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;
using Sels.Crypto.Chia.PlotBot.Models;
using System.Linq;
using Sels.Core.Components.FileSystem;
using Sels.Core.Contracts.Factory;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Components.FileSizes.Byte.Binary;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Extensions.DependencyInjection;
using Sels.Core.Components.Logging;
using Microsoft.Extensions.Logging;
using Sels.Core.Contracts.Conversion;

namespace Sels.Crypto.Chia.PlotBot
{
    public class PlotBot : IDisposable
    {
        // Fields
        private readonly IFactory<CrossPlatformDirectory> _directoryFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly IGenericTypeConverter _typeConverter;
        private readonly IPlottingService _plottingService;

        // Properties
        /// <summary>
        /// Boolean indicating if plot bot can start new instances.
        /// </summary>
        public bool CanStartNewInstances { get; set; } = true;
        public List<Plotter> Plotters { get; private set; } = new List<Plotter>();
        public List<Drive> Drives { get; private set; } = new List<Drive>();

        public PlotBot(IFactory<CrossPlatformDirectory> directoryFactory, IServiceFactory serviceFactory, IGenericTypeConverter typeConverter, IPlottingService plottingService)
        {
            _directoryFactory = directoryFactory.ValidateArgument(nameof(directoryFactory));
            _serviceFactory = serviceFactory.ValidateArgument(nameof(serviceFactory));
            _typeConverter = typeConverter.ValidateArgument(nameof(typeConverter));
            _plottingService = plottingService.ValidateArgument(nameof(plottingService));
        }

        public PlotBotResult Plot()
        {
            using var logger = LoggingServices.TraceMethod(this);
            var result = new PlotBotResult();

            // Handle completed
            using (LoggingServices.TraceAction(LogLevel.Debug, "Handling plotting results"))
            {
                var createdPlots = Plotters.SelectMany(x => x.Instances).Where(x => !x.IsPlotting).ForceSelect(x =>
                {
                    var result = x.GetResult();
                    x.Dispose();
                    return result;
                }, (x, ex) => { LoggingServices.Log($"Something went wrong when getting the result from instance: {x.Name}", ex); x.Dispose(); });

                result.CreatedPlots = createdPlots.ToArray();
            }

            // Delete completed plotters
            using (LoggingServices.TraceAction(LogLevel.Debug, "Delete completed plotters"))
            {
                var deletablePlotters = Plotters.Where(x => !x.HasRunningInstances && x.TaggedForDeletion);
                deletablePlotters.ForceExecute(x => { Plotters.Remove(x); x.Dispose(); }, (x, ex) => LoggingServices.Log($"Something went wrong when disposing {x.Alias}", ex));
                result.DeletedPlotters = deletablePlotters.Count();
            }

            if (CanStartNewInstances)
            {
                var startedInstances = new List<string>();

                // Start up new plotting instances
                using (LoggingServices.TraceAction(LogLevel.Debug, "Start plotting"))
                {
                    foreach (var plotter in Plotters.Where(x => x.CanPlotNew))
                    {
                        foreach (var drive in Drives.OrderByDescending(x => x.HasEnoughSpaceFor(plotter.PlotSize.FinalSize)).ThenBy(x => x.Priority))
                        {
                            while (plotter.CanPlotToDrive(drive))
                            {
                                startedInstances.Add(plotter.PlotToDrive(drive).Name);
                            }

                            if (!plotter.CanPlotNew)
                            {
                                break;
                            }
                        }                        
                    }
                }

                result.StartedInstances = startedInstances.ToArray();
            }

            return result;
        }

        public void ReloadConfig(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);

            LoggingServices.Log("Reloading config");

            ReloadPlotters(config);
            ReloadDrives(config);
        }

        private void ReloadPlotters(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Tag missing plotters
            foreach (var plotter in Plotters.Where(x => !config.Plotters.Any(p => p.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase))))
            {
                LoggingServices.Log($"Tagging plotter {plotter.Alias} for deletion");
                plotter.TaggedForDeletion = true;
            }

            // Add or update plotters
            foreach (var configPlotter in config.Plotters)
            {
                var existingPlotter = Plotters.FirstOrDefault(x => configPlotter.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase));

                var configPlotSize = config.Settings.PlotSizes.First(x => configPlotter.PlotSize.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                var plotSize = new PlotSize() { Name = configPlotSize.Name, CreationSize = FileSize.CreateFromSize<GibiByte>(configPlotSize.CreationSize), FinalSize = FileSize.CreateFromSize<GibiByte>(configPlotSize.FinalSize) };

                var isNew = !existingPlotter.HasValue();
                var plotter = isNew ? new Plotter(_plottingService) : existingPlotter;

                plotter.Enabled = configPlotter.Enabled;
                plotter.Alias = configPlotter.Alias;

                plotter.PoolKey = config.Settings.PoolKey;
                plotter.PoolContractAddress = config.Settings.PoolContractAddress;
                plotter.FarmerKey = config.Settings.FarmerKey;

                
                plotter.PlotSize = plotSize;
                plotter.MaxInstances = configPlotter.MaxInstances;
                plotter.TotalThreads = configPlotter.TotalThreads;
                plotter.TotalRam = configPlotter.TotalRam;
                plotter.Buckets = configPlotter.Buckets;
                plotter.PlotCommand = configPlotter.PlotCommand.HasValue() ? configPlotter.PlotCommand : config.Settings.DefaultPlotCommand;
                plotter.Caches = configPlotter.WorkingDirectories.Caches.SelectOrDefault(x => _directoryFactory.Create(x)).ToArrayOrDefault();
                plotter.WorkingDirectory = _directoryFactory.Create(configPlotter.WorkingDirectories.WorkingDirectory);
                plotter.ArchiveProgressFiles = configPlotter.WorkingDirectories.ArchiveProgressFiles;
                plotter.PlotterDelayers = configPlotter.DelaySettings.SelectOrDefault(x => _serviceFactory.Resolve<IPlotterDelayer>(x.Name).InjectProperties(x.Arguments.ToDictionary(x => x.Key, x => (object)x.Value), _typeConverter).Validate()).ToArrayOrDefault();

                plotter.TaggedForDeletion = false;

                if (isNew)
                {                 
                    Plotters.Add(plotter);
                    LoggingServices.Log($"Added plotter {plotter.Alias}");
                }
                else
                {
                    LoggingServices.Log($"Updated plotter {plotter.Alias}");
                }
            }
        }

        private void ReloadDrives(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Delete missing drives
            var drivesToDelete = Drives.Where(x => !config.Drives.Any(d => d.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase)));
            drivesToDelete.Execute(x => { Drives.Remove(x); LoggingServices.Log($"Removed drive {x.Alias}"); });

            var driveClearers = config.Settings.DriveClearers.SelectOrDefault(x => _serviceFactory.Resolve<IDriveSpaceClearer>(x.Name).InjectProperties(x.Arguments.ToDictionary(x => x.Key, x => (object)x.Value), _typeConverter).Validate()).ToArrayOrDefault();

            // Add or update drives
            foreach (var configDrive in config.Drives)
            {
                var existingDrive = Drives.FirstOrDefault(x => configDrive.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase));

                var isNew = !existingDrive.HasValue();

                var drive = isNew ? new Drive() : existingDrive;

                drive.Alias = configDrive.Alias;
                drive.Directory = _directoryFactory.Create(configDrive.Directory);
                drive.Priority = configDrive.Priority;
                drive.DriveClearers = driveClearers;
               

                if (isNew)
                {
                    Drives.Add(drive);
                    LoggingServices.Log($"Added drive {drive.Alias}");
                }
                else
                {
                    LoggingServices.Log($"Updated drive {drive.Alias}");
                }
            }

        }

        public void Dispose()
        {
            using var logger = LoggingServices.TraceMethod(this);

            Plotters.ForceExecute(x => x.Dispose(), (x, ex) => LoggingServices.Log($"Something went wrong when disposing plotter {x.Alias}", ex));

            Plotters.Clear();
            Drives.Clear();

            CanStartNewInstances = false;
        }
    }

    public class PlotBotResult
    {
        public string[] CreatedPlots { get; set; } = new string[0];
        public string[] StartedInstances { get; set; } = new string[0];
        public int DeletedPlotters { get; set; }
    }
}
