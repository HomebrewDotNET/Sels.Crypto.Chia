using Sels.Core.Components.Logging;
using Sels.Core.Extensions;
using Sels.Crypto.Chia.PlotBot.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sels.Crypto.Chia.PlotBot.Components.Services
{
    internal class WindowsPlottingService : IPlottingService
    {
        public void StartPlotting(string plotCommand, FileInfo progressFile, CancellationToken cancellationToken)
        {
            using var logger = LoggingServices.TraceMethod(this);
            plotCommand.ValidateArgument(nameof(plotCommand));
            progressFile.ValidateArgument(nameof(progressFile));

            LoggingServices.Log($"Using Window plotting service for testing. Will sleep for 1 minute");
            progressFile.Create($"Windows plotting test content: {Guid.NewGuid()}.plot");

            Task.Delay(60000, cancellationToken).Wait(cancellationToken);
        }
    }
}
