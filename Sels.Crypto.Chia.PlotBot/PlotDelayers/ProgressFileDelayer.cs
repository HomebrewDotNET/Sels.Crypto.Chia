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

namespace Sels.Crypto.Chia.PlotBot.PlotDelayers
{
    /// <summary>
    /// Delayer that checks if the progress file of the last running instance contains a certain string.
    /// </summary>
    public class ProgressFileDelayer : IPlotterDelayer
    {
        // Fields
        private readonly string _stringFilter;
        private readonly bool _isRegex;

        public ProgressFileDelayer(string filter, string isRegex) : this(filter, isRegex.ValidateArgumentNotNullOrWhitespace(nameof(isRegex)).ConvertTo<bool>())
        {

        }

        public ProgressFileDelayer(string filter) : this(filter, false)
        {

        }

        public ProgressFileDelayer(string filter, bool isRegex)
        {
            _stringFilter = filter.ValidateArgument(nameof(filter));
            _isRegex = isRegex;
        }

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

                if (_isRegex)
                {
                    allowedToPlot = Regex.IsMatch(fileContent, _stringFilter);

                    if (!allowedToPlot)
                    {
                        LoggingServices.Log(LogLevel.Debug, $"Plotter {plotter.Alias} not allowed to plot to Drive {drive.Alias}: Content of {lastStartedInstance.ProgressFile} did not match regex filter {_stringFilter}");
                    }
                }
                else
                {
                    allowedToPlot = fileContent.Contains(_stringFilter, StringComparison.OrdinalIgnoreCase);

                    if (!allowedToPlot)
                    {
                        LoggingServices.Log(LogLevel.Debug, $"Plotter {plotter.Alias} not allowed to plot to Drive {drive.Alias}: Content of {lastStartedInstance.ProgressFile} did not contain {_stringFilter}");
                    }
                }
            }

            return allowedToPlot;
        }
    }
}
