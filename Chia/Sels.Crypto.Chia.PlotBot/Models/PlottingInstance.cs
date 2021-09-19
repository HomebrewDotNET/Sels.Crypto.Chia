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
        private readonly Action<PlottingInstance> _disposeAction;
        private readonly Task _plottingTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly IPlotProgressParser _plotProgressParser;

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
        /// Time when this instance is considered timed out.
        /// </summary>
        public DateTime? TimeoutDate { get; set; }
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
        public string PlotFileName => _plotProgressParser.TrySeekPlotFileName(this, out var fileName) ? fileName : UnknownPlotFileName;
        /// <summary>
        /// Extension of plot when it is being moved.
        /// </summary>
        public string MovingPlotExtension { get; }

        /// <summary>
        /// Indicates if this instance is still creating the plot.
        /// </summary>
        public bool IsPlotting => !_plottingTask.IsCompleted;

        public PlottingInstance(int followNumber, IPlottingService plottingService, IPlotProgressParser plotFileNameSeeker, string plotCommand, int? timeout, Plotter plotter, Drive drive, FileSize reservedDestinationSize, Action<PlottingInstance> disposeAction)
        {
            FollowNumber = followNumber;
            Plotter = plotter.ValidateArgument(nameof(plotter));
            Drive = drive.ValidateArgument(nameof(drive));
            ReservedDestinationSize = reservedDestinationSize.ValidateArgument(nameof(reservedDestinationSize));
            _plotProgressParser = plotFileNameSeeker.ValidateArgument(nameof(plotFileNameSeeker));
            plottingService.ValidateArgument(nameof(plottingService));
            _disposeAction = disposeAction;
            MovingPlotExtension = plotFileNameSeeker.TransferExtension.HasValue() ? plotFileNameSeeker.TransferExtension : PlotBotConstants.Plotting.PlotFileExtension;

            plotCommand.ValidateArgumentNotNullOrWhitespace(nameof(plotCommand));
            ProgressFile = new FileInfo(Path.Combine(plotter.WorkingDirectory.Source.FullName, $"{Name}.txt"));
            ProgressFile.Write(string.Empty);

            StartTime = DateTime.Now;
            if(timeout.HasValue)
            {
                TimeoutDate = DateTime.Now.AddHours(timeout.Value);
            }

            _plottingTask = Task.Run(() => plottingService.StartPlotting(plotCommand, ProgressFile, _tokenSource.Token));
            LoggingServices.Debug($"{Name} has started plotting");
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

            using (LoggingServices.TraceAction($"Cancelling plotting instance {Name}", x => $"Cancelled plotting instance {Name} in {x.PrintTotalMs()}"))
            {
                _tokenSource.Cancel();

                while (IsPlotting)
                {
                    LoggingServices.Trace($"Cancelled Instance {Name} waiting for task to finish");
                    Thread.Sleep(250);
                }
            }           
        }

        public void Dispose()
        {
            using var logger = LoggingServices.TraceMethod(this);

            try
            {
                var stillRunning = IsPlotting;

                LoggingServices.Debug($"Instance {Name} was {(stillRunning ? "still running" : "not running")}");

                // Cancel running task
                try
                {
                    
                    // Get result from task
                    try
                    {
                        if (stillRunning)
                        {
                            LoggingServices.Debug($"Cancelling task for instance {Name}");
                            Cancel();
                            GetResult();
                        }
                    }
                    catch(Exception ex)
                    {
                        LoggingServices.Log(LogLevel.Warning, $"Error occured when cancelling running task", ex);
                    }

                    LoggingServices.Debug($"Disposing task for instance {Name}");
                    _plottingTask.TryDispose(x => LoggingServices.Log(LogLevel.Warning, $"Plotting instance {Name} could not be properly dispose of it's task", x));
                }
                catch(TaskCanceledException taskEx)
                {
                    LoggingServices.Log(LogLevel.Warning, $"Plotting instance {Name} was cancelled", taskEx);
                }

                // If the task was still running the plot was either not created yet or not fully copied
                if (stillRunning)
                {
                    LoggingServices.Debug($"Deleting moving plot for instance {Name} if it exists");
                    // Try find the plot in the destination directory and delete it
                    var plot = new FileInfo(Path.Combine(Drive.Directory.Source.FullName, Path.GetFileNameWithoutExtension(PlotFileName) + MovingPlotExtension));

                    try
                    {                        
                        if (plot.Exists)
                        {
                            plot.Delete();
                            LoggingServices.Log($"Deleted incomplete plot {plot.FullName}");
                        }
                        else
                        {
                            LoggingServices.Debug($"Could not find plot {plot.FullName} so plotter was probably not moving the file");
                        }
                    }
                    catch(Exception ex)
                    {
                        LoggingServices.Log(LogLevel.Warning, $"Could not delete incomplete plot file {plot.FullName}", ex);
                    }
                }

                // Execute dispose task if provided
                try
                {
                    LoggingServices.Debug($"Calling dispose action for instance {Name} if it exists");

                    if (_disposeAction.HasValue())
                    {
                        _disposeAction(this);
                    }
                }
                catch(Exception ex)
                {
                    LoggingServices.Log($"Something went wrong executing dispose action for instance {Name}", ex);
                }

                try
                {
                    // Archive progress file if enabled
                    if (Plotter.ArchiveProgressFiles)
                    {
                        LoggingServices.Debug($"Archiving progress file for instance {Name}");

                        var archiveDirectory = new DirectoryInfo(Path.Combine(ProgressFile.DirectoryName, ArchiveFolderName));
                        archiveDirectory.CreateIfNotExist();

                        var fileName = $"{ProgressFile.GetNameWithoutExtension()}_{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}{ProgressFile.Extension}";

                        ProgressFile.CopyTo(archiveDirectory, fileName);
                    }
                }
                catch(Exception ex)
                {
                    LoggingServices.Log($"Could not archive progress file {ProgressFile.FullName} for instance {Name}", ex);
                }

                try
                {
                    LoggingServices.Debug($"Deleting progress file for instance {Name}");

                    // Cleanup ProgressFile
                    ProgressFile.Delete();
                }
                catch (Exception ex)
                {
                    LoggingServices.Log($"Could not delete progress file {ProgressFile.FullName} for instance {Name}", ex);
                }              
            }
            finally
            {
                LoggingServices.TraceObject($"Disposed", this);
            }
        }
    }
}
