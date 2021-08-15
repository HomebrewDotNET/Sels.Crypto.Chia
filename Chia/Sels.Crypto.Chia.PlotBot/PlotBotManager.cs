using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sels.Core.Contracts.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Hashing;
using System.IO;
using Sels.Core.Components.Logging;
using Sels.Crypto.Chia.PlotBot.Models.Config;
using Sels.Core.Extensions.Conversion;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Exceptions;
using System.Security.Cryptography;
using Sels.Core.Extensions.Linq;
using Sels.Core.Components.FileSystem;
using Sels.Core.Contracts.Factory;
using Sels.Core;
using Sels.Core.Components.RecurrentAction;

namespace Sels.Crypto.Chia.PlotBot
{
    public class PlotBotManager : IHostedService
    {
        // Fields
        private readonly RepeatingActionManager<bool> _actionManager = new RepeatingActionManager<bool>();
        private readonly IPlotBotConfigValidator _configValidator;

        // State
        private string _configHash;
        private PlotBot _plotBot;

        // Properties
        /// <summary>
        /// Plot Bot configuration file.
        /// </summary>
        public FileInfo PlotBotConfigFile { get; private set; }
        /// <summary>
        /// Time in ms on how long the service waits before processing again.
        /// </summary>
        public int PollingInterval { get; private set; }

        public PlotBotManager(ILoggerFactory factory, IConfigProvider configProvider, IPlotBotConfigValidator configValidator, PlotBot plotBot)
        {
            factory.ValidateArgument(nameof(factory));
            _configValidator = configValidator.ValidateArgument(nameof(configValidator));
            _plotBot = plotBot.ValidateArgument(nameof(plotBot));

            SetupLogging(factory.CreateLogger(PlotBotConstants.LoggerName));
            LoadServiceConfig(configProvider.ValidateArgument(nameof(configProvider)));

            // Dispose plot bot when app closes
            Helper.App.RegisterApplicationClosingAction(() => {
                if (_plotBot.HasValue()) _plotBot.Dispose();
            });
        }

        protected void SetupLogging(ILogger logger)
        {
            LoggingServices.RegisterLoggers(logger);
            LoggingServices.Log(LogLevel.Debug, "Logging services has been setup.");
        }

        protected void LoadServiceConfig(IConfigProvider configProvider)
        {
            using var logger = LoggingServices.TraceMethod(this);

            using (LoggingServices.TraceAction(LogLevel.Debug, $"Loading service configuration"))
            {
                var configFile = configProvider.GetAppSetting(PlotBotConstants.Config.AppSettings.ConfigFile, true, x => x.HasValue(), x => $"Value cannot be empty or whitespace. Was <{x}>");
                PlotBotConfigFile = new FileInfo(configFile);
                PollingInterval = configProvider.GetAppSetting<int>(PlotBotConstants.Config.AppSettings.Interval, true, x => x >= 1000, x => $"Value must be equal or above 1000. Was <{x}>");
            }
        }

        protected bool Initialize()
        {
            using var logger = LoggingServices.TraceMethod(this);
            LoggingServices.Log($"{PlotBotConstants.ServiceName} is starting up for the first time. Initializing.");

            if (PlotBotConfigFile.Exists)
            {
                LoggingServices.Log($"{PlotBotConstants.ServiceName} found configuration file <{PlotBotConfigFile.FullName}>");
                var plotBotConfigFile = LoadPlotBotConfig();

                LoggingServices.Log($"Configuration file <{PlotBotConfigFile.FullName}> is valid.");
                _plotBot.ReloadConfig(plotBotConfigFile);

                return true;
            }
            else
            {
                LoggingServices.Log($"Configuration file <{PlotBotConfigFile.FullName}> does not exist. Creating file with template.");
                var defaultConfig = PlotBotConfig.Default;
                PlotBotConfigFile.Create(defaultConfig.SerializeAsJson(Newtonsoft.Json.Formatting.Indented));

                return false;
            }
        }

        protected void ExecuteAsync(CancellationToken stoppingToken)
        {
            using var logger = LoggingServices.TraceMethod(this);

            using (LoggingServices.CreateTimedLogger(LogLevel.Debug, () => $"{PlotBotConstants.ServiceName} started processing", x => $"{PlotBotConstants.ServiceName} finished processing in {x.PrintTotalMs()}"))
            {
                if (DoesConfigNeedToBeReloaded())
                {
                    try
                    {
                        var newConfig = LoadPlotBotConfig();
                        var canSafeReload = CanConfigBeSafelyReloaded(newConfig);

                        if (!canSafeReload && _plotBot.Plotters.Any(x => x.HasRunningInstances))
                        {
                            LoggingServices.Log(LogLevel.Warning, $"Config cannot be safely hot reloaded. Waiting for current plotting instances to finish so config can be fully reloaded");
                            _plotBot.CanStartNewInstances = false;
                        }
                        else
                        {
                            // Safe to reload
                            LoggingServices.Log($"{PlotBotConstants.ServiceName} can safely reload the configuration. Reloading config");
                            _plotBot.ReloadConfig(newConfig);
                            _plotBot.CanStartNewInstances = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingServices.Log(LogLevel.Warning, $"{PlotBotConstants.ServiceName} ran into issues when trying to reload config. Ignoring and continuing with old config", ex);
                    }
                }

                LoggingServices.Log($"{PlotBotConstants.ServiceName} plotting");
                var result = _plotBot.Plot();

                result.CreatedPlots.Execute(x => LoggingServices.Log($"{PlotBotConstants.ServiceName} created {x}"));

                if (result.DeletedPlotters > 0)
                {
                    LoggingServices.Log($"{PlotBotConstants.ServiceName} removed {result.DeletedPlotters} plotters");
                }

                result.StartedInstances.Execute(x => LoggingServices.Log($"{PlotBotConstants.ServiceName} started instance {x}"));
            }
        }

        private PlotBotConfig LoadPlotBotConfig()
        {
            using var logger = LoggingServices.TraceMethod(this);
            PlotBotConfigFile.ValidateArgumentExists(nameof(PlotBotConfigFile));

            LoggingServices.Log($"{PlotBotConstants.ServiceName} is reading the configuration file");
            var configFileContent = PlotBotConfigFile.Read();
            var config = configFileContent.DeserializeFromJson<PlotBotConfig>();
            LoggingServices.Log($"{PlotBotConstants.ServiceName} read configuration. Started validating configuration.");
            var errors = _configValidator.Validate(config);

            using(LoggingServices.TraceAction(LogLevel.Debug, "Generating config file hash"))
            {
                _configHash = configFileContent.GenerateHash<MD5>();
            }
            
            LoggingServices.Log($"{PlotBotConstants.ServiceName} done validating configuration");
            return !errors.HasValue() ? config : throw new PlotBotMisconfiguredException(errors);
        }

        private bool DoesConfigNeedToBeReloaded()
        {
            var configFileContent = PlotBotConfigFile.Read();
            string hash;

            LoggingServices.Log(LogLevel.Debug, $"Comparing hashes to see if config file changed");

            using (LoggingServices.TraceAction(LogLevel.Debug, "Generating config file hash"))
            {
                hash = configFileContent.GenerateHash<MD5>();
            }

            if (_configHash.Equals(hash))
            {
                LoggingServices.Log(LogLevel.Debug, $"Newly generated hash <{hash}> is the same as the old hash <{_configHash}> so no changes detected.");
            }
            else
            {
                LoggingServices.Log(LogLevel.Debug, $"Newly generated hash <{hash}> is not the same as the old hash <{_configHash}>, requesting config reload.");

                return true;
            }

            return false;
        }

        private bool CanConfigBeSafelyReloaded(PlotBotConfig newConfig)
        {
            var missingPlotters = _plotBot.Plotters.Where(x => !newConfig.Plotters.Select(p => p.Alias).Contains(x.Alias)).ToArray();
             
            if(missingPlotters.HasValue())
            {
                LoggingServices.Log($"Plotters {missingPlotters.JoinString(", ")} were missing in the new config.");

                return false;
            }

            return true;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                return Task.Run(() =>
                {
                    if (Initialize())
                    {
                        LoggingServices.Log($"{PlotBotConstants.ServiceName} successfully initialized");

                        _actionManager.AddRecurrentAction(true, PollingInterval, (x, y) => ExecuteAsync(y), (id, method, ex) => LoggingServices.Log($"{PlotBotConstants.ServiceName} ran into issue while executing", ex), null);

                        _actionManager.StartAll();
                    }
                    else
                    {
                        LoggingServices.Log($"{PlotBotConstants.ServiceName} could not be properly initialized. Service will stop. This is normal when running the service for the first time.");
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingServices.Log(LogLevel.Critical, $"{PlotBotConstants.ServiceName} could not properly start up", ex);

                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                return Task.Run(() =>
                {
                    _actionManager.StopAndWaitAll();

                    _plotBot.TryDispose(x => LoggingServices.Log($"Plot Bot could not be properly disposed", x));
                });
            }
            catch (Exception ex)
            {
                LoggingServices.Log(LogLevel.Critical, $"{PlotBotConstants.ServiceName} could not properly shutdown", ex);

                throw;
            }            
        }
    }

    public class TestPlotBotManager : PlotBotManager
    {
        public TestPlotBotManager(ILoggerFactory factory, IConfigProvider configProvider, IPlotBotConfigValidator configValidator, PlotBot plotBot) : base(factory, configProvider, configValidator, plotBot)
        {
            SendTestLogs();
        }

        private void SendTestLogs()
        {
            LoggingServices.Log(LogLevel.Information, $"This is a test message from {PlotBotConstants.ServiceName}");
            LoggingServices.Log(LogLevel.Warning, $"This is a warning test from {PlotBotConstants.ServiceName}", new Exception("Hello! I'm a test error message, no need to be alarmed!"));
            LoggingServices.Log(LogLevel.Error, $"This is an error test from {PlotBotConstants.ServiceName}", new Exception("Hello! I'm a test error message, no need to be alarmed!"));
            LoggingServices.Log(LogLevel.Critical, $"This is a fatal error mail from {PlotBotConstants.ServiceName}", new Exception("Hello! I'm a test error message, no need to be alarmed!"));
        }
    }
}
