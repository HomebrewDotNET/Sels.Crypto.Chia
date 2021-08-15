using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Components.PlotProgressParsers
{
    public abstract class BasePlotProgressParser : IPlotProgressParser
    {
        public virtual string Filter { get; set; }
        public virtual string TransferExtension { get; set; }

        public abstract bool TrySeekPlotFileName(PlottingInstance instance, out string plotFileName);

        public abstract IPlotProgressParser Validate();
    }
}
