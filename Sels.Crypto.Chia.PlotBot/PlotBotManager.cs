using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sels.Core.Contracts.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sels.Core.Extensions;
using System.IO;
using Sels.Core.Components.Logging;
using Sels.Crypto.Chia.PlotBot.Models.Config;
using Sels.Core.Extensions.Conversion;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Exceptions;

namespace Sels.Crypto.Chia.PlotBot
{
    public class PlotBotManager : BackgroundService
    {
        // Fields
        private readonly IConfigProvider _configProvider;
        private readonly IPlotBotConfigValidator _configValidator;

        // State
        private DateTime _lastConfigReloadTime;

        // Properties
        public FileInfo PlotBotConfigFile { get; }

        public PlotBotManager(ILoggerFactory factory, IConfigProvider configProvider, IPlotBotConfigValidator configValidator)
        {
            factory.ValidateArgument(nameof(factory));
            _configProvider = configProvider.ValidateArgument(nameof(configProvider));
            _configValidator = configValidator.ValidateArgument(nameof(configValidator));

            SetupLogging(factory.CreateLogger(PlotBotConstants.LoggerName));
            LoadServiceConfig(configProvider);
        }

        protected void SetupLogging(ILogger logger)
        {
            LoggingServices.RegisterLoggers(logger);
            LoggingServices.Log(LogLevel.Debug, "Logging services has been setup.");
        }

        protected void LoadServiceConfig(IConfigProvider configProvider)
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Log($"{PlotBotConstants.ServiceName} is loading in the service configuration");
        }

        protected void Initialize()
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Log($"{PlotBotConstants.ServiceName} is starting up for the first time. Initializing.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var logger = LoggingServices.TraceMethod(this);

            Initialize();

            while (!stoppingToken.IsCancellationRequested)
            {
                
                await Task.Delay(1000, stoppingToken);
            }
        }

        private PlotBotConfig LoadPlotBotConfig()
        {
            using var logger = LoggingServices.TraceMethod(this);
            PlotBotConfigFile.ValidateArgumentExists(nameof(PlotBotConfigFile));

            LoggingServices.Log($"{PlotBotConstants.ServiceName} is reading the configuration file");
            var config = PlotBotConfigFile.Read().DeserializeFromJson<PlotBotConfig>();
            LoggingServices.Log($"{PlotBotConstants.ServiceName} read configuration. Started validating configuration.");
            var errors = _configValidator.Validate(config);

            _lastConfigReloadTime = DateTime.Now;

            LoggingServices.Log($"{PlotBotConstants.ServiceName} done validating configuration");
            return !errors.HasValue() ? config : throw new PlotBotMisconfiguredException(errors);
        }
    }
}
