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

        // Fields
        private readonly Task _plottingTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly Action<PlottingInstance> _removeAction;

        // Properties
        public string Name => $"{Plotter.Alias}_{FollowNumber}_{Drive.Alias}";
        public int FollowNumber { get; set; }
        public Plotter Plotter { get; }
        public Drive Drive { get; }

        public DateTime StartTime { get; set; }
        public FileInfo ProgressFile { get; }
        public FileSize ReservedDestinationSize { get; }

        public bool IsPlotting => !_plottingTask.IsCompleted;

        public PlottingInstance(int followNumber, IPlottingService plottingService, string plotCommand, Plotter plotter, Drive drive, FileSize reservedDestinationSize, Action<PlottingInstance> registrationAction, Action<PlottingInstance> removeAction)
        {
            FollowNumber = followNumber;
            Plotter = plotter.ValidateArgument(nameof(plotter));
            Drive = drive.ValidateArgument(nameof(drive));
            ReservedDestinationSize = reservedDestinationSize.ValidateArgument(nameof(reservedDestinationSize));
            _removeAction = removeAction.ValidateArgument(nameof(removeAction));
            plottingService.ValidateArgument(nameof(plottingService));
            plotCommand.ValidateArgumentNotNullOrWhitespace(nameof(plotCommand));
            registrationAction.ValidateArgument(nameof(registrationAction));
            ProgressFile = new FileInfo(Path.Combine(plotter.WorkingDirectory.Directory.FullName, $"{Name}.txt"));
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

            return "Unknown.plot";
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
                // Cancel running task
                try
                {
                    if (IsPlotting)
                    {
                        Cancel();
                    }

                    _plottingTask.TryDispose(x => LoggingServices.Log(LogLevel.Warning, $"Plotting instance {Name} could not be properly dispose of it's task", x));
                }
                catch(TaskCanceledException taskEx)
                {
                    LoggingServices.Log(LogLevel.Warning, $"Plotting instance {Name} was canceled", taskEx);
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
