using Sels.Core.Components.Logging;
using Sels.Core.Contracts.Commands;
using Sels.Core.Linux.Commands.Bash;
using Sels.Core.Linux.Contracts.LinuxCommand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Services
{
    public class TestLinuxPlottingService : LinuxPlottingService
    {
        protected override ILinuxCommand<string> CreatePlotCommand(string plotCommand, FileInfo progressFile)
        {
            var plottingCommand = base.CreatePlotCommand(plotCommand, progressFile);

            LoggingServices.Log($"Test Mode: Plotting command would have been: {plottingCommand.BuildCommand()}");

            return new DynamicBashCommand("sleep 30s");
        }
    }
}
