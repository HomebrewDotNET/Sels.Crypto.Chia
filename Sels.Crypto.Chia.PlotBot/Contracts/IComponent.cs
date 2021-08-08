using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Contracts
{
    public interface IComponent<TSource>
    {
        /// <summary>
        /// Validates the component and throws an exception with any validation error.
        /// </summary>
        TSource Validate();
    }
}
