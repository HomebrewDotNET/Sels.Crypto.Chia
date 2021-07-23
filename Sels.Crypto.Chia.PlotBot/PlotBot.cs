using Sels.Crypto.Chia.PlotBot.Models.Config;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;

namespace Sels.Crypto.Chia.PlotBot
{
    public class PlotBot : IDisposable
    {
        // Fields

        // Properties
        public PlotBot(PlotBotConfig config)
        {
            config.ValidateArgument(nameof(config));
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
