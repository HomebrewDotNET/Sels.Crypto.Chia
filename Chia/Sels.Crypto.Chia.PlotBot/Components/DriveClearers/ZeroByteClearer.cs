using Sels.Core.Components.FileSizes.Byte;
using Sels.Core.Components.Logging;
using Sels.Core.Contracts.Factory;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Templates.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Components.DriveClearers
{
    /// <summary>
    /// Drive clearer that deletes 0 byte files. 
    /// </summary>
    public class ZeroByteClearer : BaseClearer
    {
        public ZeroByteClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
        {

        }

        public override IDriveSpaceClearer Validate()
        {
            using var logger = LoggingServices.TraceMethod(this);
            return this;
        }

        protected override bool IsClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo file)
        {
            using var logger = LoggingServices.TraceMethod(this);

            var isZeroByte = file.GetFileSize<SingleByte>() == 0;

            LoggingServices.Trace($"Found zero byte plot <{file.FullName}>");

            // Only clear if they haven't been modified in 5 minutes to avoid files that were just created.
            return isZeroByte && file.LastWriteTime < DateTime.Now.AddMinutes(-5);
        }
    }

    public class TestZeroByteClearer : ZeroByteClearer
    {
        public TestZeroByteClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
        {

        }

        protected override void HandleClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo plot, FileSize plotSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            LoggingServices.Log($"Test Mode: {plot.FullName} of size {plotSize} on drive {drive.Alias} would have been cleared to make enough space for {requiredFreeSpace}");
        }
    }
}
