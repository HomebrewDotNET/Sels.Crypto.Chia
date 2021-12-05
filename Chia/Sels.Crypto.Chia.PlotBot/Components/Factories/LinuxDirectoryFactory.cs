using Sels.Core.Components.Factory;
using Sels.Core.Components.FileSystem;
using Sels.Core.Linux.Components.FileSystem;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Templates.FileSystem;
using Sels.Core.Components.Logging;
using Sels.Core.Extensions;

namespace Sels.Crypto.Chia.PlotBot.Components.Factories
{
    public class LinuxDirectoryFactory : GenericFactory<CrossPlatformDirectory>
    {
        protected override CrossPlatformDirectory CreateNewInstance<TInstance>(params object[] arguments)
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Trace($"Creating new instance of {typeof(TInstance)} using {(arguments.HasValue() ? arguments.JoinString(";") : "no arguments")}");
            return base.CreateNewInstance<LinuxDirectory>(arguments);
        }
    }
}
