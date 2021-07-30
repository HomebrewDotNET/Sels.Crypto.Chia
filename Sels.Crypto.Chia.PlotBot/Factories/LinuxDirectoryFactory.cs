using Sels.Core.Components.Factory;
using Sels.Core.Components.FileSystem;
using Sels.Core.Linux.Components.FileSystem;
using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions.Conversion;

namespace Sels.Crypto.Chia.PlotBot.Factories
{
    public class LinuxDirectoryFactory : GenericFactory<CrossPlatformDirectory>
    {
        protected override TInstance CreateNewInstance<TInstance>(params object[] arguments)
        {
            return base.CreateNewInstance<LinuxDirectory>(arguments).As<TInstance>();
        }
    }
}
