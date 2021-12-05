using Sels.Core.Components.Factory;
using Sels.Core.Components.FileSystem;
using Sels.Core.Components.Logging;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Templates.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.Crypto.Chia.PlotBot.Components.Factories
{
    public class WindowsDirectoryFactory : GenericFactory<CrossPlatformDirectory>
    {
        protected override CrossPlatformDirectory CreateNewInstance<TInstance>(params object[] arguments)
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Trace($"Creating new instance of {typeof(WindowsDirectoryFactory)} using {(arguments.HasValue() ? arguments.JoinString(";") : "no arguments")}");
            return base.CreateNewInstance<WindowsDirectory>(arguments);
        }
    }
}
