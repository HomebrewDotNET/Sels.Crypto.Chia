using Sels.Core.Components.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sels.Core.Extensions;
using Sels.Core.Components.Logging;
using Sels.Core.Templates.FileSizes;
using System.Linq;
using Sels.Core.Components.FileSizes.Byte;
using Sels.Core.Extensions.FileSizes;
using Microsoft.Extensions.Logging;
using Sels.Core.Components.FileSizes.Byte.Binary;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Core.Templates.FileSystem;

namespace Sels.Crypto.Chia.PlotBot.Models
{
    public class Drive
    {
        // Fields
        private readonly List<PlottingInstance> _plottingInstances = new List<PlottingInstance>();

        /// <summary>
        /// Unique name to identify this drive.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Directory to fill with plots.
        /// </summary>
        public CrossPlatformDirectory Directory { get; set; }
        /// <summary>
        /// Optional priority. Used to give priority to drives so some are filled up faster.
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// Components for clearing extra drive space during plotting.
        /// </summary>
        public IDriveSpaceClearer[] DriveClearers { get; set; }

        /// <summary>
        /// Returns the available free space on this drive taking into account running plotting instances.
        /// </summary>
        public FileSize AvailableFreeSize => Directory.FreeSpace - _plottingInstances.Sum(x => x.ReservedDestinationSize.ByteSize).ToFileSize();

        /// <summary>
        /// Active instances that are plotting to this drive.
        /// </summary>
        public PlottingInstance[] Instances => _plottingInstances.ToArray();
        /// <summary>
        /// Indicates if this drive has any current instances plotting to it.
        /// </summary>
        public bool HasRunningInstances => _plottingInstances.HasValue();

        /// <summary>
        /// Checks if thid drive has enough space for <paramref name="size"/>.
        /// </summary>
        /// <param name="size">Needed free space</param>
        /// <returns>True if this drive can fit <paramref name="size"/></returns>
        public bool HasEnoughSpaceFor(FileSize size)
        {
           return AvailableFreeSize > size;
        }

        /// <summary>
        /// Checks if this drive be plotted to. Tries to free up extra space until we have enough free space for <paramref name="size"/>
        /// </summary>
        /// <param name="size">Needed free space</param>
        /// <returns>True if enough free space is available</returns>
        public bool CanBePlotted(FileSize size)
        {
            var enoughSpace = AvailableFreeSize > size;

            if (!enoughSpace && DriveClearers.HasValue())
            {
                if(DriveClearers.Any(x => x.ClearSpace(this, size)))
                {
                    LoggingServices.Log($"Drive {Alias} has cleared extra space");
                    return true;
                }
            }

            if (!enoughSpace)
            {
                LoggingServices.Log(LogLevel.Debug, $"Drive {Alias} does not have enough free size available. Needs {size.ToSize<GibiByte>()} but only has {AvailableFreeSize.ToSize<GibiByte>()}");
            }

            return enoughSpace;
        }

        internal void RegisterInstance(PlottingInstance plottingInstance)
        {
            using var logger = LoggingServices.TraceMethod(this);

            plottingInstance.ValidateArgument(nameof(plottingInstance));

            _plottingInstances.Add(plottingInstance);
        }

        internal void RemoveInstance(PlottingInstance plottingInstance)
        {
            using var logger = LoggingServices.TraceMethod(this);

            plottingInstance.ValidateArgument(nameof(plottingInstance));

            _plottingInstances.Remove(plottingInstance);
        }
    }
}
