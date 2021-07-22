using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models.Config
{
    public class PlotBotConfig
    {
        /// <summary>
        /// Global settings for the plot bot.
        /// </summary>
        public PlotBotSettingsConfig Settings { get; set; }
        /// <summary>
        /// Settings for the plotters.
        /// </summary>
        public PlotterConfig[] Plotters { get; set; }
        /// <summary>
        /// What drives to plot to.
        /// </summary>
        public DriveConfig[] Drives { get; set; }
    }
}
