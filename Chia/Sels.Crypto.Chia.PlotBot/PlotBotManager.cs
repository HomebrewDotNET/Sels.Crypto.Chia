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
using Sels.Core.Extensions.Execution;
using Sels.Core.Contracts.ScheduledAction;

namespace Sels.Crypto.Chia.PlotBot
{
    public class PlotBotManager : IHostedService
    {
        // Fields
        private readonly bool _retryOnFailedPlotting;
        private readonly bool _reduceIdleMessages;
        private readonly IScheduledAction _plotterAction;
        private readonly IScheduledAction _checkPlotterAction;
        private readonly IPlotBotConfigValidator _configValidator;

        // State
        private string _configHash;
        private PlotBot _plotBot;
        private bool _wasIdle;

        // Properties
        /// <summary>
        /// Plot Bot configuration file.
        /// </summary>
        public FileInfo PlotBotConfigFile { get; private set; }
        /// <summary>
        /// Time in ms on how long the service waits before processing again.
        /// </summary>
        public int PollingInterval { get; private set; }

        public PlotBotManager(ILoggerFactory factory, IConfigProvider configProvider, IPlotBotConfigValidator configValidator, PlotBot plotBot, IScheduledAction plotterAction, IScheduledAction checkPlotterAction, bool retryOnFailedPlotting = false, bool reduceIdleMessages = false)
        {
            factory.ValidateArgument(nameof(factory));
            _configValidator = configValidator.ValidateArgument(nameof(configValidator));
            _plotBot = plotBot.ValidateArgument(nameof(plotBot));
            _plotterAction = plotterAction.ValidateArgument(nameof(plotterAction));
            _plotterAction.Action = Plot;
            _checkPlotterAction = checkPlotterAction.ValidateArgument(nameof(checkPlotterAction));
            _checkPlotterAction.Action = CheckPlots;

            _retryOnFailedPlotting = retryOnFailedPlotting;
            _reduceIdleMessages = reduceIdleMessages;

            SetupLogging(factory.CreateLogger(PlotBotConstants.LoggerName));
            LoadServiceConfig(configProvider.ValidateArgument(nameof(configProvider)));
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
            LoggingServices.Log($"{PlotBotConstants.ServiceName} is starting up for the first time. Initializing");

            if (PlotBotConfigFile.Exists)
            {
                LoggingServices.Log($"{PlotBotConstants.ServiceName} found configuration file <{PlotBotConfigFile.FullName}>");
                var plotBotConfigFile = LoadPlotBotConfig();

                LoggingServices.Log($"Configuration file <{PlotBotConfigFile.FullName}> is valid");
                _plotBot.ReloadConfig(plotBotConfigFile);

                return true;
            }
            else
            {
                LoggingServices.Log($"Configuration file <{PlotBotConfigFile.FullName}> does not exist. Creating file with template");
                var defaultConfig = PlotBotConfig.Default;
                PlotBotConfigFile.Create(defaultConfig.SerializeAsJson(Newtonsoft.Json.Formatting.Indented));

                return false;
            }
        }

        private void Plot(CancellationToken token = default)
        {
            using var logger = LoggingServices.TraceMethod(this);

            try
            {
                using (LoggingServices.TraceAction(LogLevel.Debug, $"{PlotBotConstants.ServiceName} trying to plot", x => $"{PlotBotConstants.ServiceName} finished trying to plot in {x.PrintTotalMs()}"))
                {                  
                    var startedInstances = _plotBot.Plot(token);

                    if (startedInstances.HasValue())
                    {
                        startedInstances.Execute(x => LoggingServices.Log($"{PlotBotConstants.ServiceName} started instance {x}"));
                    }
                    else
                    {
                        LoggingServices.Debug($"No new instances started");
                    }                   
                }
            }
            catch (Exception ex)
            {
                if (_retryOnFailedPlotting)
                {
                    LoggingServices.Log(LogLevel.Error, $"{PlotBotConstants.ServiceName} ran into a fatal error when trying to plot. Will retry later", ex);
                }
                else
                {
                    LoggingServices.Log(LogLevel.Critical, $"{PlotBotConstants.ServiceName} ran into a fatal error when trying to plot. No new instances will be started", ex);
                }
            }
        }

        private void CheckPlots(CancellationToken token = default)
        {
            using var logger = LoggingServices.TraceMethod(this);

            try
            {
                using (LoggingServices.TraceAction(LogLevel.Debug, $"{PlotBotConstants.ServiceName} checking plots", x => $"{PlotBotConstants.ServiceName} finished checking plots in {x.PrintTotalMs()}"))
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

                    var result = _plotBot.HandlePlots(token);

                    result.CreatedPlots.Execute(x => LoggingServices.Log($"{PlotBotConstants.ServiceName} created {x}"));

                    if (result.DeletedPlotters > 0)
                    {
                        LoggingServices.Log($"{PlotBotConstants.ServiceName} removed {result.DeletedPlotters} plotters");
                    }

                    result.RunningInstances.Execute(x => LoggingServices.Log($"Instance {x.Name} is creating {x.PlotName} of size {x.PlotSize} and has been running for {x.StartTime.GetMinuteDifference()} minutes{(x.TimeoutDate.HasValue() ? $" and will timeout in {x.TimeoutDate.Value.GetMinuteDifference()} minutes" : "")}"));

                    var isIdle = !result.RunningInstances.HasValue();

                    if (isIdle)
                    {
                        if (_reduceIdleMessages && _wasIdle) return;

                        LoggingServices.Log($"{PlotBotConstants.ServiceName} is idle");
                        _wasIdle = true;                        
                    }
                    else
                    {
                        _wasIdle = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingServices.Log(LogLevel.Error, $"{PlotBotConstants.ServiceName} ran into a fatal error when checking up on plots", ex);
            }
        }

        private PlotBotConfig LoadPlotBotConfig()
        {
            using var logger = LoggingServices.TraceMethod(this);
            PlotBotConfigFile.ValidateArgumentExists(nameof(PlotBotConfigFile));

            LoggingServices.Log($"{PlotBotConstants.ServiceName} is reading the configuration file");
            var configFileContent = PlotBotConfigFile.Read();
            var config = configFileContent.DeserializeFromJson<PlotBotConfig>();
            LoggingServices.Log($"{PlotBotConstants.ServiceName} read configuration. Started validating configuration");
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
            using var logger = LoggingServices.TraceMethod(this);
            var configFileContent = PlotBotConfigFile.Read();
            string hash;

            LoggingServices.Log(LogLevel.Debug, $"Comparing hashes to see if config file changed");

            using (LoggingServices.TraceAction(LogLevel.Debug, "Generating config file hash"))
            {
                hash = configFileContent.GenerateHash<MD5>();
            }

            if (_configHash.Equals(hash))
            {
                LoggingServices.Log(LogLevel.Debug, $"Newly generated hash <{hash}> is the same as the old hash <{_configHash}> so no changes detected");
            }
            else
            {
                LoggingServices.Log(LogLevel.Debug, $"Newly generated hash <{hash}> is not the same as the old hash <{_configHash}>, requesting config reload");

                return true;
            }

            return false;
        }

        private bool CanConfigBeSafelyReloaded(PlotBotConfig newConfig)
        {
            using var logger = LoggingServices.TraceMethod(this);
            var missingPlotters = _plotBot.Plotters.Where(x => !newConfig.Plotters.Select(p => p.Alias).Contains(x.Alias)).ToArray();
             
            if(missingPlotters.HasValue())
            {
                LoggingServices.Log($"Plotters {missingPlotters.JoinString(", ")} were missing in the new config");

                return false;
            }

            return true;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var logger = LoggingServices.TraceMethod(this);
            return Task.Run(() =>
            {
                try
                {
                    if (Initialize())
                    {
                        LoggingServices.Log($"{PlotBotConstants.ServiceName} successfully initialized");

                        _checkPlotterAction.ExecuteAndStart();
                        _plotterAction.ExecuteAndStart();
                    }
                    else
                    {
                        var message = $"{PlotBotConstants.ServiceName} could not be properly initialized. Service will stop. This is normal when running the service for the first time.";

                        LoggingServices.Log(message);

                        throw new InvalidOperationException(message);
                    }
                }
                catch (Exception ex)
                {
                    LoggingServices.Log(LogLevel.Critical, $"{PlotBotConstants.ServiceName} could not properly start up", ex);

                    throw;
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            using var logger = LoggingServices.TraceMethod(this);
            return Task.Run(() =>
            {
                try
                {
                    using(LoggingServices.TraceAction($"Shutting down {PlotBotConstants.ServiceName}", x => $"Shut down {PlotBotConstants.ServiceName} in {x.PrintTotalMs()}"))
                    {
                        if(_plotterAction.IsRunning) _plotterAction.Stop();
                        if (_checkPlotterAction.IsRunning) _checkPlotterAction.Stop();

                        _plotBot.TryDispose(x => LoggingServices.Log($"Plot Bot could not be properly disposed", x));
                    }                   
                }
                catch (Exception ex)
                {
                    LoggingServices.Log(LogLevel.Critical, $"{PlotBotConstants.ServiceName} could not properly shutdown", ex);

                    throw;
                }
            });
        }
    }

    public class TestPlotBotManager : PlotBotManager
    {
        public TestPlotBotManager(ILoggerFactory factory, IConfigProvider configProvider, IPlotBotConfigValidator configValidator, PlotBot plotBot, IScheduledAction plotterAction, IScheduledAction checkPlotterAction, bool retryOnFailedPlotting = false, bool reduceIdleMessages = false) : base(factory, configProvider, configValidator, plotBot, plotterAction, checkPlotterAction, retryOnFailedPlotting, reduceIdleMessages)
        {
            SendTestLogs();
        }

        private void SendTestLogs()
        {
            LoggingServices.Log(LogLevel.Information, $"This is a test message from {PlotBotConstants.ServiceName}");
            LoggingServices.Log(LogLevel.Warning, $"This is a warning test from {PlotBotConstants.ServiceName}", new Exception("Hello! I'm a test error message, no need to be alarmed!"));
            LoggingServices.Log(LogLevel.Error, $"This is an error test from {PlotBotConstants.ServiceName}", new Exception("Hello! I'm a test error message, no need to be alarmed!"));
            LoggingServices.Log(LogLevel.Critical, $"This is a fatal error test from {PlotBotConstants.ServiceName}", new Exception("Hello! I'm a test error message, no need to be alarmed!"));
        }
    }
}
