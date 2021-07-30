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

        public FileSize AvailableFreeSize => Directory.FreeSpace - _plottingInstances.Sum(x => x.ReservedDestinationSize.ByteSize).ToFileSize();

        /// <summary>
        /// Active instances that are plotting to this drive.
        /// </summary>
        public PlottingInstance[] Instances => _plottingInstances.ToArray();
        /// <summary>
        /// Indicates if this drive has any current instances plotting to it.
        /// </summary>
        public bool HasRunningInstances => _plottingInstances.HasValue();

        public bool HasEnoughSpaceFor(FileSize size)
        {
            var enoughSpace = AvailableFreeSize > size;

            if (!enoughSpace)
            {
                // Todo: Components that can clear space like deleting non-nft plots.
            }

            enoughSpace = AvailableFreeSize > size;

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
