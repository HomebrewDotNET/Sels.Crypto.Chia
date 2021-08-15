using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    /// <summary>
    /// Component that extracts information from the progress file
    /// </summary>
    public interface IPlotProgressParser : IComponent<IPlotProgressParser>
    {
        /// <summary>
        /// Extension that uses when the plot is being moved.
        /// </summary>
        public string TransferExtension { get; set; }

        /// <summary>
        /// Tries to seek the file name of the plot that's being/was created.
        /// </summary>
        /// <param name="instance">Instance that is creating the plot</param>
        /// <param name="plotFileName">File name of the plot if it is found</param>
        /// <returns>Boolean indicating if we found the plot file name</returns>
        public bool TrySeekPlotFileName(PlottingInstance instance, out string plotFileName);
    }
}
