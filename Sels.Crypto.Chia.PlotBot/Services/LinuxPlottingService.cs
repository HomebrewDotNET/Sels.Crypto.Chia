using Sels.Crypto.Chia.PlotBot.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sels.Core.Extensions;
using Sels.Core.Components.Logging;
using Sels.Core.Linux.Commands.Bash;
using Sels.Core.Linux.Commands.Core;
using Sels.Core.Contracts.Commands;
using Sels.Core.Linux.Contracts.LinuxCommand;
using Microsoft.Extensions.Logging;
using Sels.Core.Linux.Exceptions.LinuxCommand;

namespace Sels.Crypto.Chia.PlotBot.Services
{
    public class LinuxPlottingService : IPlottingService
    {
        public void StartPlotting(string plotCommand, FileInfo progressFile, CancellationToken cancellationToken = default)
        {
            using var logger = LoggingServices.TraceMethod(this);
            plotCommand.ValidateArgument(nameof(plotCommand));
            progressFile.ValidateArgument(nameof(progressFile));

            var plottingCommand = CreatePlotCommand(plotCommand, progressFile);

            LoggingServices.Log(LogLevel.Trace, $"Executing following plotting command: {plottingCommand.BuildCommand()}");

            if(!plottingCommand.RunCommand(out var output, out var error, out var exitCode))
            {
                throw new LinuxCommandExecutionFailedException(error);
            }

            LoggingServices.Log(LogLevel.Trace, $"Plot command exited with {exitCode} and result {output}");
        }

        protected virtual ILinuxCommand<string> CreatePlotCommand(string plotCommand, FileInfo progressFile)
        {
            plotCommand.ValidateArgument(nameof(plotCommand));
            progressFile.ValidateArgument(nameof(progressFile));

            // Execute plot command with bash
            var plottingCommand = new DynamicCommand(plotCommand);
            // Write progress to file with tee
            var teeCommand = new TeeCommand(progressFile.FullName);
            // Redirect error to output file
            var redirectError = new DynamicCommand("2>&1");
            // Chain command together
            var commandChain = new ChainCommand(plottingCommand, CommandChain.None, redirectError, CommandChain.Pipe, teeCommand);

            return commandChain;
        }
    }
}
