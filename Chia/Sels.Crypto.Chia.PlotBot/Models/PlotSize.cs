using Sels.Core.Templates.FileSizes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models
{
    public class PlotSize
    {
        /// <summary>
        /// Name of the plot size.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Max filesize that a plot takes up when being created.
        /// </summary>
        public FileSize CreationSize { get; set; }
        /// <summary>
        /// How large the final plot size is.
        /// </summary>
        public FileSize FinalSize { get; set; }
    }
}
