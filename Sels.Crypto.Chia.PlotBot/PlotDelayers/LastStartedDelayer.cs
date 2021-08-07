using Sels.Crypto.Chia.PlotBot.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using Sels.Crypto.Chia.PlotBot.Models;
using System.Linq;
using Sels.Core.Components.Logging;
using Microsoft.Extensions.Logging;

namespace Sels.Crypto.Chia.PlotBot.PlotDelayers
{
    /// <summary>
    /// Delayer that checks when the last instance was started before starting another instance.
    /// </summary>
    public class LastStartedDelayer : IPlotterDelayer
    {
        // Properties
        /// <summary>
        /// How long to wait after the last instance was started before starting a new one.
        /// </summary>
        public int MinuteDelay { get; set; }

        public LastStartedDelayer()
        {
        }

        public bool CanStartInstance(Plotter plotter, Drive drive)
        {
            using var loggers = LoggingServices.TraceMethod(this);

            plotter.ValidateArgument(nameof(plotter));
            drive.ValidateArgument(nameof(drive));

            var allowedToPlot = true;

            if(plotter.HasRunningInstances && MinuteDelay != 0)
            {
                var lastRunningInstance = plotter.Instances.OrderByDescending(x => x.StartTime).First();
                var allowedTimeToRun = DateTime.Now.AddMinutes(MinuteDelay.ToNegative());

                allowedToPlot =  allowedTimeToRun > lastRunningInstance.StartTime;

                if (!allowedToPlot)
                {
                    LoggingServices.Log(LogLevel.Debug, $"Plotter {plotter.Alias} not allowed to plot to Drive {drive.Alias}: Instance {lastRunningInstance.Name} started less than {MinuteDelay} minutes ago");
                }
            }

            return allowedToPlot;
        }

        public IPlotterDelayer Validate()
        {
            return this;
        }
    }
}
