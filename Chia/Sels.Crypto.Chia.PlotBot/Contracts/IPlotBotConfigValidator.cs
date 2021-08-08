using Sels.Crypto.Chia.PlotBot.Models.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    public interface IPlotBotConfigValidator
    {
        IEnumerable<string> Validate(PlotBotConfig config);
    }
}
