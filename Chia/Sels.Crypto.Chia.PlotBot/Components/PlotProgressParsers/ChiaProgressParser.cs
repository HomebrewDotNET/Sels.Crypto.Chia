using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Components.PlotProgressParsers
{
    /// <summary>
    /// Progress Parser for the official Chia plotter
    /// </summary>
    public class ChiaProgressParser : StringPlotProgressParser
    {
        public override string Filter { get; set; } = PlotBotConstants.Components.PlotProgressParser.ChiaFilterArg;
        public override string TransferExtension { get; set; } = PlotBotConstants.Components.PlotProgressParser.ChiaTransferExtensionArg;
    }
}
