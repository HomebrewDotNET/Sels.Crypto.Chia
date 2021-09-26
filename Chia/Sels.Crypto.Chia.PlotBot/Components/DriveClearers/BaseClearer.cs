using Sels.Core.Contracts.Factory;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Templates.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;
using System.Linq;
using Sels.Core.Components.Logging;
using System.IO;
using Microsoft.Extensions.Logging;
using Sels.Core.Components.FileSizes.Byte;
using Sels.Core.Components.FileSizes.Byte.Binary;

namespace Sels.Crypto.Chia.PlotBot.Components.DriveClearers
{
    /// <summary>
    /// Base class with common properties and methods.
    /// </summary>
    public abstract class BaseClearer : IDriveSpaceClearer
    {
        // Constants
        private const string DriveSplitter = ";";
        private const string PlotFilter = "*.plot";

        // Fields
        protected string _loggerName;
        private string _additionalDrives;
        private CrossPlatformDirectory[] _additionalDirectories;
        private readonly IFactory<CrossPlatformDirectory> _directoryFactory;

        // Properties
        public string AdditionalDrives
        {
            get
            {
                return _additionalDrives;
            }
            set
            {
                _additionalDrives = value;
                if (_additionalDrives.HasValue())
                {
                    _additionalDirectories = _additionalDrives.Split(DriveSplitter, StringSplitOptions.RemoveEmptyEntries).Select(x => _directoryFactory.Create(x.Trim())).ToArray();
                }
            }
        }

        public BaseClearer(IFactory<CrossPlatformDirectory> directoryFactory)
        {
            _loggerName = GetType().Name;
            _directoryFactory = directoryFactory.ValidateArgument(nameof(directoryFactory));
        }

        public virtual bool ClearSpace(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            drive.ValidateArgument(nameof(drive));
            requiredSize.ValidateArgument(nameof(requiredSize));

            var freeSpace = drive.AvailableFreeSize;
            FileSize deletedSize = new SingleByte(0);

            LoggingServices.Trace($"Drive {drive.Alias} has {freeSpace.ToSize<GibiByte>()} free");

            foreach (var clearableFile in GetClearableFiles(drive, requiredSize))
            {
                var fileSize = clearableFile.GetFileSize<GibiByte>();

                HandleClearableFile(drive, requiredSize, clearableFile, fileSize);
                deletedSize += fileSize;

                if (deletedSize + freeSpace >= requiredSize)
                {
                    LoggingServices.Log($"Freed up {deletedSize.ToSize<GibiByte>()} on Drive {drive.Alias}");

                    return true;
                }
            }

            return false;
        }

        protected IEnumerable<FileInfo> GetClearableFiles(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Seach drive root first
            LoggingServices.Debug($"Seaching drive {drive.Alias} for clearable files");
            var plots = GetClearableFiles(drive, requiredSize, drive.Directory);

            foreach (var plot in plots)
            {
                yield return plot;
            }

            if (AdditionalDrives.HasValue())
            {
                var mointPoint = drive.Directory.MountPoint;
                LoggingServices.Debug($"Seaching additional drives for drive {drive.Alias} that have the same mount point. Moint point is <{mointPoint}>");
                var matchingDirectories = _additionalDirectories.Where(x => x.Exists && x.MountPoint.Equals(mointPoint)).ToArray();

                if (matchingDirectories.HasValue())
                {
                    foreach (var directory in matchingDirectories)
                    {
                        LoggingServices.Debug($"Seaching additional drive {directory.FullName} for clearable files");

                        foreach (var plot in GetClearableFiles(drive, requiredSize, directory))
                        {
                            yield return plot;
                        }
                    }
                }
                else
                {
                    LoggingServices.Debug($"None of the additional drives share a moint point with drive {drive.Alias}");
                }
            }
        }

        protected IEnumerable<FileInfo> GetClearableFiles(Drive drive, FileSize requiredSize, CrossPlatformDirectory directory)
        {
            using var logger = LoggingServices.TraceMethod(this);

            var plots = directory.Source.GetFiles(PlotFilter, SearchOption.AllDirectories);

            LoggingServices.Log(LogLevel.Debug, $"{_loggerName} found {plots.Length} plots to check");

            if (plots.HasValue())
            {
                foreach (var plot in plots)
                {
                    if (IsClearablePlot(drive, requiredSize, plot))
                    {
                        yield return plot;
                    }
                }
            }
        }

        protected virtual void HandleClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo plot, FileSize plotSize)
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Log($"Clearing {plot.FullName} of size {plotSize} on drive {drive.Alias}");
            plot.Delete();
        }

        // Abstractions
        protected abstract bool IsClearablePlot(Drive drive, FileSize requiredFreeSpace, FileInfo plot);
        public abstract IDriveSpaceClearer Validate();
    }
}
