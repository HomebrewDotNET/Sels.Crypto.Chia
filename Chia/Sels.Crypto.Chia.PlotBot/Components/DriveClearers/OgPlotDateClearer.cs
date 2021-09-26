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
    /// Clears og plots by checking the date in the file name.
    /// </summary>
    public class OgPlotDateClearer : BaseClearer
    {
        // Constants
        private const int StartIndex = 9;
        private const int DateLength = 16;
        private const string DateTimeFormat = "yyyy-MM-dd-HH-mm";

        /// <summary>
        /// Plots older than this date can be deleted.
        /// </summary>
        public DateTime ThresHold { get; set; }

        public OgPlotDateClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
        {
        }
      
        public override IDriveSpaceClearer Validate()
        {
            using var logger = LoggingServices.TraceMethod(this);
            return this;
        }

        protected override bool IsClearablePlot(Drive drive, FileSize requiredFreeSpace, FileInfo plot)
        {
            using var logger = LoggingServices.TraceMethod(this);

            var datePart = plot.Name.Substring(StartIndex, DateLength);

            LoggingServices.Trace($"Extracted date section {datePart} from plot {plot.FullName}");

            if (DateTime.TryParseExact(datePart, DateTimeFormat, null, DateTimeStyles.None, out var date))
            {
                if (date < ThresHold)
                {
                    LoggingServices.Trace($"Plot {plot.FullName} was created on {date} and passed the threshold date of {ThresHold}");
                    return true;
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

            return false;
        }
    }

    public class TestOgPlotDateClearer : OgPlotDateClearer
    {
        public TestOgPlotDateClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
        {

        }

        protected override void HandleClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo plot, FileSize plotSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            LoggingServices.Log($"Test Mode: {plot.FullName} of size {plotSize} on drive {drive.Alias} would have been cleared to make enough space for {requiredFreeSpace}");
        }
    }
}
