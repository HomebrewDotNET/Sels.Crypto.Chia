using Sels.Core.Components.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models
{
    public class Drive
    {
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
    }
}
