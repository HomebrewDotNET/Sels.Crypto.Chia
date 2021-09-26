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
        /// <summary>
        /// Additional drives to check when searching for clearable files
        /// </summary>
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

        /// <summary>
        /// Filter used to search for files by matching the file name against this filter.
        /// </summary>
        protected virtual string FileFilter => PlotFilter;

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

            LoggingServices.Trace($"Drive {drive.Alias} has {freeSpace.ToSize(PlotBotConstants.Logging.DefaultLoggingFileSize)} free");

            var deleteFiles = 0;
            foreach (var clearableFile in GetClearableFiles(drive, requiredSize))
            {
                var fileSize = clearableFile.GetFileSize<GibiByte>();

                HandleClearableFile(drive, requiredSize, clearableFile, fileSize);
                deleteFiles++;
                deletedSize += fileSize;

                if (deletedSize + freeSpace >= requiredSize)
                {
                    LoggingServices.Log($"Freed up {deletedSize.ToSize(PlotBotConstants.Logging.DefaultLoggingFileSize)} on Drive {drive.Alias} by deleting <{deleteFiles}> files");

                    return true;
                }
            }

            LoggingServices.Debug($"Could not free up {requiredSize.ToSize(PlotBotConstants.Logging.DefaultLoggingFileSize)} for Drive {drive.Alias} after deleting <{deleteFiles}> files");

            return false;
        }

        protected IEnumerable<FileInfo> GetClearableFiles(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Seach drive root first
            LoggingServices.Debug($"Seaching drive {drive.Alias} for clearable files");
            var files = GetClearableFiles(drive, requiredSize, drive.Directory);

            foreach (var file in files)
            {
                yield return file;
            }

            if (_additionalDirectories.HasValue())
            {
                var mointPoint = drive.Directory.MountPoint;
                LoggingServices.Debug($"Seaching additional drives for drive {drive.Alias} that have the same mount point. Moint point is <{mointPoint}>");
                var matchingDirectories = _additionalDirectories.Where(x => x.Exists && x.MountPoint.Equals(mointPoint)).ToArray();

                if (matchingDirectories.HasValue())
                {
                    foreach (var directory in matchingDirectories)
                    {
                        LoggingServices.Debug($"Seaching additional drive {directory.FullName} for clearable files");

                        foreach (var file in GetClearableFiles(drive, requiredSize, directory))
                        {
                            yield return file;
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

            var files = directory.Source.GetFiles(FileFilter, SearchOption.AllDirectories);

            LoggingServices.Log(LogLevel.Debug, $"{_loggerName} found {files.Length} files to check");

            if (files.HasValue())
            {
                foreach (var file in files)
                {
                    if (IsClearableFile(drive, requiredSize, file))
                    {
                        yield return file;
                    }
                }
            }
        }

        protected virtual void HandleClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo file, FileSize fileSize)
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Log($"Clearing {file.FullName} of size {fileSize} on drive {drive.Alias}");
            file.Delete();
        }

        // Abstractions
        protected abstract bool IsClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo file);
        public abstract IDriveSpaceClearer Validate();
    }
}
