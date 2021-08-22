using Sels.Core.Components.Logging;
using Sels.Core.Contracts.Commands;
using Sels.Core.Linux.Components.LinuxCommand.Commands.Bash;
using Sels.Core.Linux.Contracts.LinuxCommand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Sels.Crypto.Chia.PlotBot.Services
{
    public class TestLinuxPlottingService : LinuxPlottingService
    {
        protected override ILinuxCommand CreatePlotCommand(string plotCommand, FileInfo progressFile, CancellationToken cancellationToken)
        {
            using var logger = LoggingServices.TraceMethod(this);
            var plottingCommand = base.CreatePlotCommand(plotCommand, progressFile, cancellationToken);

            LoggingServices.Log($"Test Mode: Plotting command would have been: {plottingCommand.BuildCommand()}");

            return new DynamicBashCommand("sleep 60s") {
                CancellationToken = cancellationToken
            };
        }
    }
}
