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

namespace Sels.Crypto.Chia.PlotBot.DriveClearers
{
    /// <summary>
    /// Clears old plots by checking the the date in the file name.
    /// </summary>
    public class OldPlotDateClearer : IDriveSpaceClearer
    {
        // Constants
        private const string PlotFilter = "*.plot";
        private const int StartIndex = 9;
        private const int DateLength = 19;
        private const string DateTimeFormat = "yyyy-MM-dd-hh-mm-ss";

        // Properties
        /// <summary>
        /// Plots oldeer than this can be deleted.
        /// </summary>
        public DateTime ThresHold { get; set; }

        public virtual bool ClearSpace(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            drive.ValidateArgument(nameof(drive));
            requiredSize.ValidateArgument(nameof(requiredSize));

            FileSize deletedSize = new SingleByte(0);

            foreach(var clearableFile in GetClearableFiles(drive, requiredSize))
            {
                var fileSize = clearableFile.GetFileSize<GibiByte>();
                LoggingServices.Log($"Clearing {clearableFile.FullName} of size {fileSize} on drive {drive.Alias}");

                clearableFile.Delete();
                deletedSize += fileSize;

                if(deletedSize > requiredSize)
                {
                    break;
                }
            }

            if(deletedSize.ByteSize > 0)
            {
                LoggingServices.Log($"Freed up {deletedSize.ToSize<GibiByte>()} on Drive {drive.Alias}");
            }

            return drive.AvailableFreeSize > requiredSize;
        }

        public IEnumerable<FileInfo> GetClearableFiles(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            var plots = drive.Directory.Directory.GetFiles(PlotFilter, SearchOption.AllDirectories);

            if (plots.HasValue())
            {
                var currentIndex = 0;

                while (currentIndex < plots.Length && drive.AvailableFreeSize < requiredSize)
                {
                    var plotFile = plots[currentIndex];

                    var datePart = plotFile.Name.Substring(StartIndex, DateLength);

                    if (DateTime.TryParseExact(datePart, DateTimeFormat, null, DateTimeStyles.None, out var date) && date < ThresHold)
                    {
                        yield return plotFile;
                    }
                    currentIndex++;
                }
            }
        }

        public IDriveSpaceClearer Validate()
        {
            return this;
        }
    }
}
