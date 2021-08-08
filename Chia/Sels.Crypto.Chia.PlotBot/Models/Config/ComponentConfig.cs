using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models.Config
{
    public class ComponentConfig
    {
        /// <summary>
        /// Name of the component.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Arguments for the component.
        /// </summary>
        public Dictionary<string, string> Arguments { get; set; }

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static ComponentConfig DefaultPlotterDelayer => new ComponentConfig()
        {
            Name = PlotBotConstants.Components.Delay.ProgressFileContains,
            Arguments = new Dictionary<string, string>()
            {
                { PlotBotConstants.Components.Delay.ProgressFileContainsFilter, PlotBotConstants.Components.Delay.ProgressFileContainsFilterDefaultArg }
            }
        };

        /// <summary>
        /// Default instance.
        /// </summary>
        public static ComponentConfig DefaultDriveClearer => new ComponentConfig()
        {
            Name = PlotBotConstants.Components.Clearer.OgDate,
            Arguments = new Dictionary<string, string>()
            {
                { PlotBotConstants.Components.Clearer.OgDateThreshold, PlotBotConstants.Components.Clearer.OgDateThresholdArg }
            }
        };
    }
}
