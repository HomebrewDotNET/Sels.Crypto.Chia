using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models.Config
{
    /// <summary>
    /// Contains settings about drives the plotter will fill with plots.
    /// </summary>
    public class DriveConfig
    {
        /// <summary>
        /// Unique name to identify this drive.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Directory to fill with plots.
        /// </summary>
        public string Directory { get; set; }
        /// <summary>
        /// Optional priority. Used to give priority to drives so some are filled up faster.
        /// </summary>
        public int Priority { get; set; } = PlotBotConstants.Settings.Drive.DefaultPriority;
    }
}
