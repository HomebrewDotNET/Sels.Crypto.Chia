using Sels.Core.Templates.FileSizes;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    /// <summary>
    /// Tries to clear extra space on a drive when it is full
    /// </summary>
    public interface IDriveSpaceClearer : IComponent<IDriveSpaceClearer>
    {
        bool ClearSpace(Drive drive, FileSize requiredSize);
    }
}
