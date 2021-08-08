using Sels.Core.Components.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sels.Core.Extensions;
using Sels.Core.Contracts.Commands;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Components.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Microsoft.Extensions.Logging;

namespace Sels.Crypto.Chia.PlotBot.Models
{
    public class PlottingInstance : IDisposable
    {
        // Constants
        public const string ArchiveFolderName = "Archive";
        public const string UnknownPlotFileName = "Unknown" + PlotBotConstants.Plotting.PlotFileExtension;

        // Fields
        private readonly Task _plottingTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly Action<PlottingInstance> _removeAction;
        private readonly IPlotFileNameSeeker _plotFileNameSeeker;

        // Properties
        /// <summary>
        /// Unique name for this instance.
        /// </summary>
        public string Name => $"{Plotter.Alias}_{FollowNumber}_{Drive.Alias}";
        /// <summary>
        /// Unique follow number received from Plotter.
        /// </summary>
        public int FollowNumber { get; set; }
        /// <summary>
        /// Plotter that created the instance.
        /// </summary>
        public Plotter Plotter { get; }
        /// <summary>
        /// Destination drive for the created plot.
        /// </summary>
        public Drive Drive { get; }

        /// <summary>
        /// Time when plotting started.
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// File containing the output of the plotting command.
        /// </summary>
        public FileInfo ProgressFile { get; }
        /// <summary>
        /// Reserved file size of the plot that's being created.
        /// </summary>
        public FileSize ReservedDestinationSize { get; }
        /// <summary>
        /// File name of the plot that was created. Returns <see cref="UnknownPlotFileName"/> when the seeker can't find the plot file name.
        /// </summary>
        public string PlotFileName => _plotFileNameSeeker.TrySeekPlotFileName(this, out var fileName) ? fileName : UnknownPlotFileName;

        /// <summary>
        /// Indicates if this instance is still creating the plot.
        /// </summary>
        public bool IsPlotting => !_plottingTask.IsCompleted;

        public PlottingInstance(int followNumber, IPlottingService plottingService, IPlotFileNameSeeker plotFileNameSeeker, string plotCommand, Plotter plotter, Drive drive, FileSize reservedDestinationSize, Action<PlottingInstance> registrationAction, Action<PlottingInstance> removeAction)
        {
            FollowNumber = followNumber;
            Plotter = plotter.ValidateArgument(nameof(plotter));
            Drive = drive.ValidateArgument(nameof(drive));
            ReservedDestinationSize = reservedDestinationSize.ValidateArgument(nameof(reservedDestinationSize));
            _removeAction = removeAction.ValidateArgument(nameof(removeAction));
            _plotFileNameSeeker = plotFileNameSeeker.ValidateArgument(nameof(plotFileNameSeeker));
            plottingService.ValidateArgument(nameof(plottingService));
            
            plotCommand.ValidateArgumentNotNullOrWhitespace(nameof(plotCommand));
            registrationAction.ValidateArgument(nameof(registrationAction));
            ProgressFile = new FileInfo(Path.Combine(plotter.WorkingDirectory.Source.FullName, $"{Name}.txt"));
            ProgressFile.Write(string.Empty);

            StartTime = DateTime.Now;
            _plottingTask = Task.Run(() => plottingService.StartPlotting(plotCommand, ProgressFile, _tokenSource.Token));
            registrationAction(this);
            LoggingServices.Log($"{Name} has started plotting");
        }

        /// <summary>
        /// Returns the name of the created plot.
        /// </summary>
        /// <returns>Name of created plot</returns>
        public string GetResult()
        {
            using var logger = LoggingServices.TraceMethod(this);

            IsPlotting.ValidateArgument(x => !x, x => new InvalidOperationException("Plotting task is still running"));

            _plottingTask.Wait();

            return PlotFileName;
        }

        /// <summary>
        /// Cancels the plotting instance and waits till it stopped.
        /// </summary>
        public void Cancel()
        {
            using var logger = LoggingServices.TraceMethod(this);

            LoggingServices.Log($"Canceling plotting instance {Name}");
            _tokenSource.Cancel();

            while (IsPlotting)
            {
                Thread.Sleep(250);
            }
        }

        public void Dispose()
        {
            using var logger = LoggingServices.TraceMethod(this);

            try
            {
                var stillRunning = IsPlotting;

                // Cancel running task
                try
                {
                    if (stillRunning)
                    {
                        Cancel();
                    }

                    _plottingTask.TryDispose(x => LoggingServices.Log(LogLevel.Warning, $"Plotting instance {Name} could not be properly dispose of it's task", x));
                }
                catch(TaskCanceledException taskEx)
                {
                    LoggingServices.Log(LogLevel.Warning, $"Plotting instance {Name} was canceled", taskEx);
                }

                // If the task was still running the plot was either not created yet or not fully copied
                if (stillRunning)
                {
                    // Try find the plot in the destination directory and delete it
                    var plot = new FileInfo(Path.Combine(Drive.Directory.Source.FullName, PlotFileName));

                    try
                    {                        
                        if (plot.Exists)
                        {
                            plot.Delete();
                        }
                    }
                    catch(Exception ex)
                    {
                        LoggingServices.Log(LogLevel.Warning, $"Could not delete incomplete plot file {plot.FullName}", ex);
                    }
                }
                
                // Archive progress file if enabled
                if (Plotter.ArchiveProgressFiles)
                {
                    var archiveDirectory = new DirectoryInfo(Path.Combine(ProgressFile.DirectoryName, ArchiveFolderName));
                    archiveDirectory.CreateIfNotExist();

                    var fileName = $"{ProgressFile.GetNameWithoutExtension()}_{DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss")}{ProgressFile.Extension}";

                    ProgressFile.CopyTo(archiveDirectory, fileName);
                }

                // Cleanup ProgressFile
                ProgressFile.Delete();
            }
            finally
            {
                _removeAction(this);
                LoggingServices.Trace($"Disposed", this);
            }
        }
    }
}
