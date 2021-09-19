using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Exceptions;
using Sels.Crypto.Chia.PlotBot.Models;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Sels.Core;
using System.Linq;
using Sels.Core.Components.Logging;

namespace Sels.Crypto.Chia.PlotBot.Components.PlotProgressParsers
{
    /// <summary>
    /// Parser that searches for words that contains the filter.
    /// </summary>
    public class StringPlotProgressParser : BasePlotProgressParser
    {
        public override bool TrySeekPlotFileName(PlottingInstance instance, out string plotFileName)
        {
            using var logger = LoggingServices.TraceMethod(this);
            instance.ValidateArgument(nameof(instance));
            plotFileName = string.Empty;

            var progressFileContent = instance.ProgressFile.Read();

            // Check file content for words matching the regex filter
            if (progressFileContent.HasValue())
            {
                LoggingServices.Debug($"Looking for words that contain {Filter}");
                foreach (var match in progressFileContent.Split().Where(x => x.Contains(Filter)))
                {
                    var matchedFileName = match;
                    LoggingServices.Debug($"Found word match {matchedFileName}");

                    // Append extension if it's missing
                    if (!matchedFileName.EndsWith(PlotBotConstants.Plotting.PlotFileExtension))
                    {
                        LoggingServices.Debug($"String match {matchedFileName} did not end with {PlotBotConstants.Plotting.PlotFileExtension}. Adding it");
                        matchedFileName += PlotBotConstants.Plotting.PlotFileExtension;
                    }

                    matchedFileName = Path.GetFileName(matchedFileName);

                    if (matchedFileName.HasValue() && Helper.FileSystem.IsValidFileName(matchedFileName))
                    {
                        plotFileName = matchedFileName;
                        LoggingServices.Debug($"Found plot file name {plotFileName} matching word filter {Filter}");
                        return true;
                    }
                    else
                    {
                        LoggingServices.Debug($"Found plot file name {plotFileName} but was not a valid file name");
                    }
                }
            }


            return false;
        }

        public override IPlotProgressParser Validate()
        {
            using var logger = LoggingServices.TraceMethod(this);
            if (!Filter.HasValue())
            {
                throw new PlotBotMisconfiguredException($"{nameof(Filter)} cannot be null, empty or whitespace");
            }

            return this;
        }
    }
}
