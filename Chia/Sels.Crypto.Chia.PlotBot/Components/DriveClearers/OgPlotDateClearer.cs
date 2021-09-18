using Microsoft.Extensions.Logging;
using Sels.Core.Components.FileSizes.Byte;
using Sels.Core.Components.FileSizes.Byte.Binary;
using Sels.Core.Components.Logging;
using Sels.Core.Contracts.Factory;
using Sels.Core.Extensions;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Templates.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Components.DriveClearers
{
    /// <summary>
    /// Clears old plots by checking the the date in the file name.
    /// </summary>
    public class OgPlotDateClearer : IDriveSpaceClearer
    {
        // Constants
        private const string Name = "Og Plot Date Clearer";
        private const string PlotFilter = "*.plot";
        private const int StartIndex = 9;
        private const int DateLength = 16;
        private const string DateTimeFormat = "yyyy-MM-dd-HH-mm";
        private const string DriveSplitter = ";";

        // Fields
        private string _additionalDrives;
        private CrossPlatformDirectory[] _additionalDirectories;
        private readonly IFactory<CrossPlatformDirectory> _directoryFactory;

        // Properties
        public string AdditionalDrives { 
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
        /// Plots older than this date can be deleted.
        /// </summary>
        public DateTime ThresHold { get; set; }

        public OgPlotDateClearer(IFactory<CrossPlatformDirectory> directoryFactory)
        {
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

            foreach(var clearableFile in GetClearableFiles(drive))
            {
                var fileSize = clearableFile.GetFileSize<GibiByte>();
                LoggingServices.Log($"Clearing {clearableFile.FullName} of size {fileSize} on drive {drive.Alias}");

                clearableFile.Delete();
                deletedSize += fileSize;

                if(deletedSize+freeSpace >= requiredSize)
                {
                    LoggingServices.Log($"Freed up {deletedSize.ToSize<GibiByte>()} on Drive {drive.Alias}");

                    return true;
                }
            }
           
            return false;
        }

        public IEnumerable<FileInfo> GetClearableFiles(Drive drive)
        {
            using var logger = LoggingServices.TraceMethod(this);

            // Seach drive root first
            LoggingServices.Debug($"Seaching drive {drive.Alias} for clearable files");
            var plots = GetClearableFiles(drive.Directory);

            foreach(var plot in plots)
            {
                yield return plot;
            }

            if (_additionalDirectories.HasValue())
            {
                var mointPoint = drive.Directory.MountPoint;
                LoggingServices.Debug($"Seaching additional drives for drive {drive.Alias} that have the same mount point. Moint point is <{mointPoint}>");
                var matchingDirectories = _additionalDirectories.Where(x => x.Exists && x.MountPoint.Equals(mointPoint)).ToArray();

                if (matchingDirectories.HasValue())
                {
                    foreach(var directory in matchingDirectories)
                    {
                        LoggingServices.Debug($"Seaching additional drive {directory.FullName} for clearable files");

                        foreach(var plot in GetClearableFiles(directory))
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

        public IEnumerable<FileInfo> GetClearableFiles(CrossPlatformDirectory directory)
        {
            using var logger = LoggingServices.TraceMethod(this);

            var plots = directory.Source.GetFiles(PlotFilter, SearchOption.AllDirectories);

            LoggingServices.Log(LogLevel.Debug, $"{Name} found {plots.Length} plots to check");

            if (plots.HasValue())
            {
                foreach (var plot in plots)
                {
                    var datePart = plot.Name.Substring(StartIndex, DateLength);

                    LoggingServices.Trace($"Extracted date section {datePart} from plot {plot.FullName}");

                    if (DateTime.TryParseExact(datePart, DateTimeFormat, null, DateTimeStyles.None, out var date))
                    {
                        if (date < ThresHold)
                        {
                            LoggingServices.Trace($"Plot {plot.FullName} was created on {date} and passed the threshold date of {ThresHold}");
                            yield return plot;
                        }
                        else
                        {
                            LoggingServices.Trace($"Plot {plot.FullName} was created on {date} and did not pass the threshold date of {ThresHold}");
                        }
                    }
                    else
                    {
                        LoggingServices.Warning($"Could not convert date section from Plot {plot.FullName}");
                    }
                }
            }
        }

        public IDriveSpaceClearer Validate()
        {
            using var logger = LoggingServices.TraceMethod(this);
            return this;
        }
    }

    public class TestOgPlotDateClearer : OgPlotDateClearer
    {
        public TestOgPlotDateClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
        {

        }

        public override bool ClearSpace(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            drive.ValidateArgument(nameof(drive));
            requiredSize.ValidateArgument(nameof(requiredSize));

            var freeSpace = drive.AvailableFreeSize;
            FileSize deletedSize = new SingleByte(0);

            LoggingServices.Trace($"Drive {drive.Alias} has {freeSpace.ToSize<GibiByte>()} free");

            foreach (var clearableFile in GetClearableFiles(drive))
            {
                var fileSize = clearableFile.GetFileSize<GibiByte>();

                LoggingServices.Log($"Test Mode: {clearableFile.FullName} of size {fileSize} on drive {drive.Alias} would have been cleared to make enough space for {requiredSize}");

                deletedSize += fileSize;

                if (deletedSize + freeSpace >= requiredSize)
                {
                    LoggingServices.Log($"Test Mode: Would have freed up {deletedSize.ToSize<GibiByte>()} on Drive {drive.Alias}");

                    return true;
                }
            }

            return false;
        }
    }
}
