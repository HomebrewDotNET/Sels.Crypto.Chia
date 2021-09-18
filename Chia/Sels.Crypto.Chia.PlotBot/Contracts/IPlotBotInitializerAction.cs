using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    /// <summary>
    /// Allows for the execution of an action with the plotters and drives after plotbot initialized for the first time.
    /// </summary>
    public interface IPlotBotInitializerAction
    {
        /// <summary>
        /// Performs an action with <paramref name="plotters"/> or <paramref name="drives"/> after plot bot has initialized.
        /// </summary>
        /// <param name="plotters">Plotter that plot bot loading in</param>
        /// <param name="drives">Drives that plot bot loaded in</param>
        void Handle(Plotter[] plotters, Drive[] drives);
    }
}
