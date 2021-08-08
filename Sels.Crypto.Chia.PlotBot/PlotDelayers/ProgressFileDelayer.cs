using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Sels.Core.Components.Logging;
using Microsoft.Extensions.Logging;
using Sels.Crypto.Chia.PlotBot.Exceptions;

namespace Sels.Crypto.Chia.PlotBot.PlotDelayers
{
    /// <summary>
    /// Delayer that checks if the progress file of the last running instance contains a certain string.
    /// </summary>
    public class ProgressFileDelayer : IPlotterDelayer
    {
        // Properties
        /// <summary>
        /// Filters used to check the progress file
        /// </summary>
        public string Filter { get; set; }
        /// <summary>
        /// Indicates if <see cref="Filter"/> is a regex filter
        /// </summary>
        public bool IsRegex { get; set; }


        public bool CanStartInstance(Plotter plotter, Drive drive)
        {
            using var loggers = LoggingServices.TraceMethod(this);

            plotter.ValidateArgument(nameof(plotter));
            drive.ValidateArgument(nameof(drive));

            var allowedToPlot = true;

            if (plotter.HasRunningInstances)
            {
                var lastStartedInstance = plotter.Instances.OrderByDescending(x => x.StartTime).First();
                var fileContent = lastStartedInstance.ProgressFile.Read();

                if (IsRegex)
                {
                    allowedToPlot = Regex.IsMatch(fileContent, Filter);

                    if (!allowedToPlot)
                    {
                        LoggingServices.Log(LogLevel.Debug, $"Plotter {plotter.Alias} not allowed to plot to Drive {drive.Alias}: Content of {lastStartedInstance.ProgressFile} did not match regex filter {Filter}");
                    }
                }
                else
                {
                    allowedToPlot = fileContent.Contains(Filter, StringComparison.OrdinalIgnoreCase);

                    if (!allowedToPlot)
                    {
                        LoggingServices.Log(LogLevel.Debug, $"Plotter {plotter.Alias} not allowed to plot to Drive {drive.Alias}: Content of {lastStartedInstance.ProgressFile} did not contain {Filter}");
                    }
                }
            }

            return allowedToPlot;
        }

        public IPlotterDelayer Validate()
        {
            if (!Filter.HasValue())
            {
                throw new PlotBotMisconfiguredException($"{nameof(Filter)} cannot be null, empty or whitespace");
            }

            return this;
        }
    }
}
