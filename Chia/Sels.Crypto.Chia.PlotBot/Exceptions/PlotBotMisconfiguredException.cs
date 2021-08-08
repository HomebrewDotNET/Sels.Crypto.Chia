using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;

namespace Sels.Crypto.Chia.PlotBot.Exceptions
{
    public class PlotBotMisconfiguredException : Exception
    {
        // Constants
        private const string MessageFormat = "Plot Bot configuration file contained errors: {0}";

        // Properties
        public string[] Errors { get; }

        public PlotBotMisconfiguredException(IEnumerable<string> errors) : base(MessageFormat.FormatString(Environment.NewLine + errors.ValidateArgument(nameof(errors)).JoinStringNewLine()))
        {
            Errors = errors.ToArray();
        }

        public PlotBotMisconfiguredException(string error) : this(error.ValidateArgument(nameof(error)).AsArray())
        {
        }
    }
}
