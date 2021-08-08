using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    /// <summary>
    /// Component responsible for extracting the plot file name from a plotting instance so plot bot knows which file is being/was created.
    /// </summary>
    public interface IPlotFileNameSeeker : IComponent<IPlotFileNameSeeker>
    {
        /// <summary>
        /// Tries to seek the file name of the plot that's being/was created.
        /// </summary>
        /// <param name="instance">Instance that is creating the plot</param>
        /// <param name="plotFileName">File name of the plot if it is found</param>
        /// <returns>Boolean indicating if we found the plot file name</returns>
        public bool TrySeekPlotFileName(PlottingInstance instance, out string plotFileName);
    }
}
