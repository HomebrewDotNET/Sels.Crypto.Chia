using Sels.Crypto.Chia.PlotBot.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sels.Core.Extensions;
using Sels.Core.Components.Logging;
using Sels.Core.Contracts.Commands;
using Sels.Core.Linux.Contracts.LinuxCommand;
using Microsoft.Extensions.Logging;
using Sels.Core.Linux.Exceptions.LinuxCommand;
using Sels.Core.Linux.Components.LinuxCommand.Commands.Core;
using Sels.Core.Linux.Components.LinuxCommand.Commands;
using Sels.Core.Linux.Extensions;

namespace Sels.Crypto.Chia.PlotBot.Services
{
    public class LinuxPlottingService : IPlottingService
    {
        public void StartPlotting(string plotCommand, FileInfo progressFile, CancellationToken cancellationToken)
        {
            using var logger = LoggingServices.TraceMethod(this);
            plotCommand.ValidateArgument(nameof(plotCommand));
            progressFile.ValidateArgument(nameof(progressFile));

            var plottingCommand = CreatePlotCommand(plotCommand, progressFile, cancellationToken);
            
            LoggingServices.Log(LogLevel.Trace, $"Executing following plotting command: {plottingCommand.BuildCommand()}");

            var plottingResult = plottingCommand.Execute();

            LoggingServices.Log(LogLevel.Trace, $"Plot command exited with {plottingResult.ExitCode}");

            plottingResult.GetResult();
        }

        protected virtual ILinuxCommand CreatePlotCommand(string plotCommand, FileInfo progressFile, CancellationToken cancellationToken)
        {
            using var logger = LoggingServices.TraceMethod(this);
            plotCommand.ValidateArgument(nameof(plotCommand));
            progressFile.ValidateArgument(nameof(progressFile));

            // Execute plot command with shell
            var plottingCommand = new DynamicCommand(plotCommand);
            // Write progress to file with tee
            var teeCommand = new TeeCommand(progressFile.FullName);
            // Redirect error to output file
            var redirectError = new DynamicCommand("2>&1");
            // Chain command together
            var commandChain = new ChainCommand(plottingCommand, CommandChainer.None, redirectError, CommandChainer.Pipe, teeCommand)
            {
                CancellationToken = cancellationToken
            };
            return commandChain;
        }
    }
}
