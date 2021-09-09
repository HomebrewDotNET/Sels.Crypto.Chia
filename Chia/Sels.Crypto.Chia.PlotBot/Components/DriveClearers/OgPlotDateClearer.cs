using Microsoft.Extensions.Logging;
using Sels.Core.Components.FileSizes.Byte;
using Sels.Core.Components.FileSizes.Byte.Binary;
using Sels.Core.Components.Logging;
using Sels.Core.Extensions;
using Sels.Core.Templates.FileSizes;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        // Properties
        /// <summary>
        /// Plots older than this date can be deleted.
        /// </summary>
        public DateTime ThresHold { get; set; }

        public virtual bool ClearSpace(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            drive.ValidateArgument(nameof(drive));
            requiredSize.ValidateArgument(nameof(requiredSize));

            var freeSpace = drive.AvailableFreeSize;
            FileSize deletedSize = new SingleByte(0);

            LoggingServices.Trace($"Drive {drive.Alias} has {freeSpace.ToSize<GibiByte>()} free");

            foreach(var clearableFile in GetClearableFiles(drive, requiredSize))
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

        public IEnumerable<FileInfo> GetClearableFiles(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            var plots = drive.Directory.Source.GetFiles(PlotFilter, SearchOption.AllDirectories);

            LoggingServices.Log(LogLevel.Debug, $"{Name} found {plots.Length} plots to check");

            if (plots.HasValue())
            {
                foreach (var plot in plots)
                {
                    var datePart = plot.Name.Substring(StartIndex, DateLength);

                    LoggingServices.Debug($"Extracted date section {datePart} from plot {plot.FullName}");

                    if (DateTime.TryParseExact(datePart, DateTimeFormat, null, DateTimeStyles.None, out var date))
                    {
                        if(date < ThresHold)
                        {
                            LoggingServices.Debug($"Plot {plot.FullName} was created on {date} and passed the threshold date of {ThresHold}");
                            yield return plot;
                        }
                        else
                        {
                            LoggingServices.Debug($"Plot {plot.FullName} was created on {date} and did not pass the threshold date of {ThresHold}");
                        }                       
                    }
                    else
                    {
                        LoggingServices.Debug($"Could not convert date section from Plot {plot.FullName}");
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
        public override bool ClearSpace(Drive drive, FileSize requiredSize)
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
