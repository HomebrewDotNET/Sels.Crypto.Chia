using Sels.Core.Components.Logging;
using Sels.Core.Templates.FileSizes;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;
using Sels.Core.Components.FileSizes.Byte;
using Sels.Core.Components.FileSizes.Byte.Binary;
using System.IO;

namespace Sels.Crypto.Chia.PlotBot.DriveClearers
{
    public class TestOldPlotDateClearer : OldPlotDateClearer
    {
        public override bool ClearSpace(Drive drive, FileSize requiredSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            drive.ValidateArgument(nameof(drive));
            requiredSize.ValidateArgument(nameof(requiredSize));

            FileSize deletedSize = new SingleByte(0);

            foreach (var clearableFile in GetClearableFiles(drive, requiredSize))
            {
                var fileSize = clearableFile.GetFileSize<GibiByte>();

                LoggingServices.Log($"Test Mode: {clearableFile.FullName} of size {fileSize} on drive {drive.Alias} would have been cleared to make enough space for {requiredSize}");

                deletedSize += fileSize;

                if (deletedSize > requiredSize)
                {
                    break;
                }              
            }

            if (deletedSize.ByteSize > 0)
            {
                LoggingServices.Log($"Test Mode: Would have freed up {deletedSize.ToSize<GibiByte>()} on Drive {drive.Alias}");
            }

            return drive.AvailableFreeSize > requiredSize;
        }
    }
}
