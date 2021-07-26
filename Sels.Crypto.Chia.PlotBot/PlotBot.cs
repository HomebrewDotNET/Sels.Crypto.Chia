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
using Sels.Core.Components.Logging;

namespace Sels.Crypto.Chia.PlotBot
{
    public class PlotBot : IDisposable
    {
        // Fields
        private readonly IFactory<CrossPlatformDirectory> _directoryFactory;
        private readonly IObjectFactory _objectFactory;


        // Properties
        /// <summary>
        /// Boolean indicating if plot bot can start new instances.
        /// </summary>
        public bool CanStartNewInstances { get; set; } = true;
        public List<Plotter> Plotters { get; private set; } = new List<Plotter>();
        public List<Drive> Drives { get; private set; } = new List<Drive>();

        public PlotBot(IFactory<CrossPlatformDirectory> directoryFactory, IObjectFactory objectFactory)
        {
            _directoryFactory = directoryFactory.ValidateArgument(nameof(directoryFactory));
            _objectFactory = objectFactory.ValidateArgument(nameof(objectFactory));
        }


        public void ReloadConfig(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);

            ReloadPlotters(config);
            ReloadDrives(config);
        }

        private void ReloadPlotters(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Tag missing plotters
            foreach (var plotter in Plotters.Where(x => !config.Plotters.Any(p => p.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase))))
            {
                plotter.TaggedForDeletion = true;
            }

            // Add or update plotters
            foreach (var configPlotter in config.Plotters)
            {
                var existingPlotter = Plotters.FirstOrDefault(x => configPlotter.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase));

                var configPlotSize = config.Settings.PlotSizes.First(x => configPlotter.PlotSize.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                var plotSize = new PlotSize() { Name = configPlotSize.Name, CreationSize = FileSize.CreateFromSize<GibiByte>(configPlotSize.CreationSize), FinalSize = FileSize.CreateFromSize<GibiByte>(configPlotSize.FinalSize) };

                var isNew = !existingPlotter.HasValue();
                var plotter = isNew ? new Plotter() : existingPlotter;

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
                plotter.PlotterDelayers = configPlotter.DelaySettings.SelectOrDefault(x => _objectFactory.Build<IPlotterDelayer>(x.Name, x.Arguments)).ToArrayOrDefault();

                plotter.TaggedForDeletion = false;

                if (isNew)
                {
                    Plotters.Add(plotter);
                }
            }
        }

        private void ReloadDrives(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Delete missing drives
            var drivesToDelete = Drives.Where(x => !config.Drives.Any(d => d.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase)));
            drivesToDelete.Execute(x => Drives.Remove(x));

            // Add or update drives
            foreach (var configDrive in config.Drives)
            {
                var existingDrive = Drives.FirstOrDefault(x => configDrive.Alias.Equals(x.Alias, StringComparison.OrdinalIgnoreCase));

                var isNew = !existingDrive.HasValue();

                var drive = isNew ? new Drive() : existingDrive;

                drive.Directory = _directoryFactory.Create(configDrive.Directory);
                drive.Priority = configDrive.Priority;
            }

        }

        public void Dispose()
        {
            using var logger = LoggingServices.TraceMethod(this);
        }
    }
}
