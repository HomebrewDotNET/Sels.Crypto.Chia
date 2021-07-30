using System;
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

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotBotSettingsConfig Default => new PlotBotSettingsConfig()
        {
            PoolKey = "MyPoolKey",
            PoolContractAddress = "MyPoolContractAddress",
            FarmerKey = "MyFarmerKey",
            DefaultPlotCommand = PlotBotConstants.Settings.DefaultCommand,
            PlotSizes = new PlotSizeConfig[] { PlotSizeConfig.Default }
        };

        internal object First()
        {
            throw new NotImplementedException();
        }
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
        public decimal CreationSize { get; set; }
        /// <summary>
        /// How large the final plot size is in GiB.
        /// </summary>
        public decimal FinalSize { get; set; }

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotSizeConfig Default => new PlotSizeConfig()
        {
            Name = PlotBotConstants.Settings.Plotters.DefaultPlotSize,
            CreationSize = 220,
            FinalSize = 101.4M
        };
    }
}
