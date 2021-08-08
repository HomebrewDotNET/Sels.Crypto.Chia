using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    public interface IPlottingService
    {
        void StartPlotting(string plotCommand, FileInfo progressFile, CancellationToken cancellationToken = default);
    }
}
