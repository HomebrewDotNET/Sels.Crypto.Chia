using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    public interface IPlotterDelayer
    {
        /// <summary>
        /// Checks if <paramref name="plotter"/> is allowed to start a new instance on <paramref name="drive"/>.
        /// </summary>
        /// <param name="plotter">Plotter that want to start a new instance</param>
        /// <param name="drive">Drive that <paramref name="plotter"/> want to plot to</param>
        /// <returns>Boolean indicating if <paramref name="plotter"/> can start plotting to <paramref name="drive"/></returns>
        bool CanStartInstance(Plotter plotter, Drive drive);
    }
}
