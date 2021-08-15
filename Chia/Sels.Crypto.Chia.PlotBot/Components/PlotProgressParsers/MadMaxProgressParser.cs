using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Components.PlotProgressParsers
{
    /// <summary>
    /// Progress Parser for the MadMax plotter
    /// </summary>
    public class MadMaxProgressParser : StringPlotProgressParser
    {
        public override string Filter { get; set; } = PlotBotConstants.Components.PlotProgressParser.MadMaxFilterArg;
        public override string TransferExtension { get; set; } = PlotBotConstants.Components.PlotProgressParser.MadMaxTransferExtensionArg;
    }
}
