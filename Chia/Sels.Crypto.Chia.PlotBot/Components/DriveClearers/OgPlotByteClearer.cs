using Sels.Core.Components.Logging;
using Sels.Core.Contracts.Factory;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Templates.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sels.Core.Extensions.Conversion;

namespace Sels.Crypto.Chia.PlotBot.Components.DriveClearers
{
    /// <summary>
    /// Clears og plots by checking bytes in the plot header.
    /// </summary>
    public class OgPlotByteClearer : BaseClearer
    {
        // Constants
        private const int StartByteOffset = 52;

        public OgPlotByteClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
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

            using(var reader = new BinaryReader(plot.OpenRead()))
            {
                const int readLength = 2;          
                int offset = 0;
                LoggingServices.Trace($"Opening stream for <{plot.FullName}>");

                // 1000 bytes should give enough headroom
                byte[] buffer = new byte[readLength];

                // Read format length in plot header to calculate byte location for memo
                LoggingServices.Trace($"Reading format length from byte stream for <{plot.FullName}>");
                offset = StartByteOffset;
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                reader.Read(buffer);
                var formatLength = GetLength(buffer);
                LoggingServices.Trace($"Format length for <{plot.FullName}> is <{formatLength}>");

                // Read memo length from plot header
                LoggingServices.Trace($"Reading memo length from byte stream for <{plot.FullName}>");
                offset = StartByteOffset + readLength + formatLength;
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                reader.Read(buffer);
                var memoLength = GetLength(buffer);
                LoggingServices.Trace($"Memo length for <{plot.FullName}> is <{formatLength}>");

                // Try convert memo length to plot type
                var plotType = PlotType.Unknown;

                if(Enum.IsDefined(typeof(PlotType), memoLength))
                {
                    plotType = memoLength.As<PlotType>();
                }

                LoggingServices.Trace($"Plot type for <{plot.FullName}> is <{plotType}>");

                switch (plotType)
                {
                    case PlotType.Og:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private int GetLength(byte[] bytes)
        {
            return bytes[1] >> bytes[0];
        }
    }

    public class TestOgPlotByteClearer : OgPlotByteClearer
    {
        public TestOgPlotByteClearer(IFactory<CrossPlatformDirectory> directoryFactory) : base(directoryFactory)
        {

        }

        protected override void HandleClearableFile(Drive drive, FileSize requiredFreeSpace, FileInfo plot, FileSize plotSize)
        {
            using var logger = LoggingServices.TraceMethod(this);

            LoggingServices.Log($"Test Mode: {plot.FullName} of size {plotSize} on drive {drive.Alias} would have been cleared to make enough space for {requiredFreeSpace}");
        }
    }

    internal enum PlotType
    {
        Unknown = 0,
        Og = 128,
        Nft = 112
    }
}
