﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models.Config
{
    public class PlotBotSettingsConfig
    {
        /// <summary>
        /// Public pool key.
        /// </summary>
        public string PoolKey { get; set; }
        /// <summary>
        /// Pool contract address.
        /// </summary>
        public string PoolContractAddress { get; set; }
        /// <summary>
        /// Public farmer key.
        /// </summary>
        public string FarmerKey { get; set; }


        /// <summary>
        /// Default command that starts a new process that creates plots. This command is used when it's not defined in the <see cref="PlotterConfig"/>.
        /// </summary>
        public string DefaultPlotCommand { get; set; }

        /// <summary>
        /// Available plot sizes that the plotters can create.
        /// </summary>
        public PlotSizeConfig[] PlotSizes { get; set; }

    }

    public class PlotSizeConfig
    {
        /// <summary>
        /// Name of the plot size.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Max Size in GiB that a plot takes up when being created.
        /// </summary>
        public double CreationSize { get; set; }
        /// <summary>
        /// How large the final plot size is in GiB.
        /// </summary>
        public double FinalSize { get; set; }
    }
}
