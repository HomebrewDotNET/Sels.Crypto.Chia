using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sels.Core.Components.Configuration;
using Sels.Core.Contracts.Configuration;
using Sels.Core.Contracts.Factory;
using Sels.Core.Unity.Components.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Exceptions.Configuration;
using Sels.Core;
using System.IO;
using NLog.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using Sels.Core.Templates.FileSizes;
using Sels.Core.Components.FileSizes.Byte;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.ValidationProfiles;
using Sels.Core.Components.FileSystem;
using Sels.Crypto.Chia.PlotBot.Factories;
using Sels.Core.Components.Factory;
using Sels.Core.Components.Conversion;
using Sels.Crypto.Chia.PlotBot.Services;
using Microsoft.Extensions.Configuration;
using NLog.Common;
using Sels.Core.Components.IoC;
using Sels.Core.Contracts.Conversion;
using Sels.Core.Templates.FileSystem;
using Sels.Crypto.Chia.PlotBot.Components.PlotDelayers;
using Sels.Crypto.Chia.PlotBot.Components.DriveClearers;
using Sels.Core.Extensions.Linq;
using System.Net.Mail;
using Sels.Core.Components.Logging;
using Sels.Crypto.Chia.PlotBot.Components.PlotProgressParsers;

namespace Sels.Crypto.Chia.PlotBot
{
    public class Program
    {
        // Constants
        private const string ConfigProviderKey = "ConfigProvider";

        public static void Main(string[] args)
        {
            try
            {
                NLog.LogManager.AutoShutdown = false;
                // Set appsetting directory
                if (args.Length > 0)
                {
                    Directory.SetCurrentDirectory(args[0]);
                }
                else
                {
                    Helper.App.SetCurrentDirectoryToExecutingAssembly();
                }

                CreateHostBuilder(args).Build().Run();
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    // Replace default IConfiguration because publishing breaks the default path
                    services.AddSingleton(x => Helper.App.BuildDefaultConfigurationFile());

                    // Register services
                    services.AddSingleton<PlotBot>();
                    services.AddSingleton<IConfigProvider, ConfigProvider>();
                    services.AddSingleton<IPlotBotConfigValidator, ConfigValidationProfile>();
                    services.AddSingleton<IFactory<CrossPlatformDirectory>, LinuxDirectoryFactory>();
                    services.AddSingleton<IObjectFactory, AliasTypeFactory>(x => {
                        return new AliasTypeFactory(x.GetRequiredService<IConfigProvider>(), GenericConverter.DefaultConverter);
                    });

                    services.AddSingleton<IGenericTypeConverter, GenericConverter>(x => {
                        var converter = GenericConverter.DefaultConverter;
                        converter.Settings.ThrowOnFailedConversion = false;
                        return converter;
                    });

                    // Create config provider
                    var provider = services.BuildServiceProvider();
                    var configProvider = provider.GetRequiredService<IConfigProvider>();

                    // Check test mode services
                    var testMode = configProvider.GetAppSetting<bool>(PlotBotConstants.Config.AppSettings.TestMode, false);

                    if (testMode)
                    {
                        services.AddHostedService<TestPlotBotManager>();
                        services.AddSingleton<IPlottingService, TestLinuxPlottingService>();
                    }
                    else
                    {
                        services.AddHostedService<PlotBotManager>();
                        services.AddSingleton<IPlottingService, LinuxPlottingService>();
                    }

                    // Setup service factory
                    services.AddSingleton<IServiceFactory, UnityServiceFactory>(x => {
                        var factory = new UnityServiceFactory();
                        factory.LoadFrom(services);

                        // Progress parsers
                        factory.Register<IPlotProgressParser, StringPlotProgressParser>(ServiceScope.Scoped, PlotBotConstants.Components.PlotProgressParser.String);
                        factory.Register<IPlotProgressParser, RegexPlotProgressParser>(ServiceScope.Scoped, PlotBotConstants.Components.PlotProgressParser.Regex);
                        factory.Register<IPlotProgressParser, MadMaxProgressParser>(ServiceScope.Scoped, PlotBotConstants.Components.PlotProgressParser.MadMax);
                        factory.Register<IPlotProgressParser, ChiaProgressParser>(ServiceScope.Scoped, PlotBotConstants.Components.PlotProgressParser.Chia);

                        // Plotter delayers
                        factory.Register<IPlotterDelayer, LastStartedDelayer>(ServiceScope.Scoped, PlotBotConstants.Components.Delay.TimeStarted);
                        factory.Register<IPlotterDelayer, ProgressFileDelayer>(ServiceScope.Scoped, PlotBotConstants.Components.Delay.ProgressFileContains);

                        // Drive clearers
                        if (testMode)
                        {
                            factory.Register<IDriveSpaceClearer, TestOgPlotDateClearer>(ServiceScope.Scoped, PlotBotConstants.Components.Clearer.OgDate);
                        }
                        else
                        {
                            factory.Register<IDriveSpaceClearer, OgPlotDateClearer>(ServiceScope.Scoped, PlotBotConstants.Components.Clearer.OgDate);
                        }

                        return factory;
                    });

                    // Setup logging
                    services.AddLogging(x => SetupLogging(configProvider, x, testMode));
                });

        private static void SetupLogging(IConfigProvider configProvider, ILoggingBuilder builder, bool isTestMode)
        {
            // Read config
            var devMode = configProvider.GetAppSetting<bool>(PlotBotConstants.Config.AppSettings.DevMode, false);
            var minLogLevel = configProvider.GetSectionSetting<LogLevel>(PlotBotConstants.Config.LogSettings.MinLogLevel, nameof(PlotBotConstants.Config.LogSettings));
            var logDirectory = configProvider.GetSectionSetting(PlotBotConstants.Config.LogSettings.LogDirectory, nameof(PlotBotConstants.Config.LogSettings), true, x => x.HasValue() && Directory.Exists(x), x => $"Directory cannot be empty and Directory must exist on the file system. Was <{x}>");
            var archiveSize = configProvider.GetSectionSetting<long>(PlotBotConstants.Config.LogSettings.ArchiveSize, nameof(PlotBotConstants.Config.LogSettings), true, x => x > 1, x => $"File size cannot be empty and file size must be above 1 {MegaByte.FileSizeAbbreviation}");
            var archiveFileSize = FileSize.CreateFromSize<MegaByte>(archiveSize);
            var isDebug = minLogLevel <= LogLevel.Debug;

            // Logging config
            var mailingEnabled = configProvider.IsSectionDefined(nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
            LogLevel minMailLogLevel = LogLevel.Warning;
            string mailSender = string.Empty;
            string mailReceivers = string.Empty;
            string server = string.Empty;
            int port = 1;
            string username = string.Empty;
            string password = string.Empty;
            bool isSsl = false;

            // Read mail config if defined
            if (mailingEnabled)
            {
                minMailLogLevel = configProvider.GetSectionSetting<LogLevel>(PlotBotConstants.Config.LogSettings.Mail.MinLogLevel, true, null, null, nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                mailSender = configProvider.GetSectionSetting(PlotBotConstants.Config.LogSettings.Mail.Sender, true, HasStringValue, x => CreateConfigValueEmptyMessage(PlotBotConstants.Config.LogSettings.Mail.Sender, x), nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                mailReceivers = configProvider.GetSectionSetting(PlotBotConstants.Config.LogSettings.Mail.Receivers, true, HasStringValue, null, nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                server = configProvider.GetSectionSetting(PlotBotConstants.Config.LogSettings.Mail.Server, true, HasStringValue, x => CreateConfigValueEmptyMessage(PlotBotConstants.Config.LogSettings.Mail.Server, x), nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                port = configProvider.GetSectionSetting<int>(PlotBotConstants.Config.LogSettings.Mail.Port, true, x => x > 0, x => $"Port must be above 0. Was {x}", nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                username = configProvider.GetSectionSetting(PlotBotConstants.Config.LogSettings.Mail.Username, true, HasStringValue, x => CreateConfigValueEmptyMessage(PlotBotConstants.Config.LogSettings.Mail.Username, x), nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                password = configProvider.GetSectionSetting(PlotBotConstants.Config.LogSettings.Mail.Password, true, HasStringValue, x => CreateConfigValueEmptyMessage(PlotBotConstants.Config.LogSettings.Mail.Password, x), nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
                isSsl = configProvider.GetSectionSetting<bool>(PlotBotConstants.Config.LogSettings.Mail.Ssl, true, null, null, nameof(PlotBotConstants.Config.LogSettings), nameof(PlotBotConstants.Config.LogSettings.Mail));
            }


            var logDirectoryInfo = new DirectoryInfo(logDirectory);
            var minLogLevelOrdinal = minLogLevel.ConvertTo<int>();

            // Enable nlog internal logging if in dev mode
            if (devMode)
            {
                InternalLogger.LogToConsole = true;
                InternalLogger.LogFile = Path.Combine(logDirectory, "Nlog.txt");
                InternalLogger.LogLevel = NLog.LogLevel.Debug;
            }

            // Clear providers and set basic settings
            builder.ClearProviders();
            builder.SetMinimumLevel(minLogLevel);

            var config = new LoggingConfiguration();

            // Create targets
            if(isDebug) config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotDebug, logDirectoryInfo, archiveFileSize));
            config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotAll, logDirectoryInfo, archiveFileSize));
            config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotError, logDirectoryInfo, archiveFileSize, PlotBotConstants.Logging.FullLayout));
            config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotCritical, logDirectoryInfo, archiveFileSize, PlotBotConstants.Logging.FullLayout));

            // Create rules
            var nlogMinLevel = NLog.LogLevel.FromOrdinal(minLogLevelOrdinal);
            // Skip microsoft logs
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Info, new NullTarget(), PlotBotConstants.Logging.Categories.Microsoft, true);
            // Debug logs
            if(isDebug) config.AddRule(nlogMinLevel, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotDebug);
            // All logs
            config.AddRule(isDebug ? NLog.LogLevel.Info : nlogMinLevel, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotAll);
            // All errors
            config.AddRule(minLogLevelOrdinal >= NLog.LogLevel.Warn.Ordinal ? nlogMinLevel : NLog.LogLevel.Warn, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotError);
            // Fatal errors only
            config.AddRule(NLog.LogLevel.Fatal, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotCritical);

            // Create mail logging
            if (mailingEnabled)
            {
                config.AddTarget(new MailTarget()
                {
                    Name = PlotBotConstants.Logging.Targets.PlotBotMail,
                    SmtpAuthentication = SmtpAuthenticationMode.Basic,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Subject = PlotBotConstants.Logging.MailSubjectLayout,
                    Body = PlotBotConstants.Logging.MailBodyLayout,
                    From = mailSender,
                    To = mailReceivers,
                    Html = false,
                    SmtpServer = server,
                    SmtpPort = port,
                    SmtpUserName = username,
                    SmtpPassword = password,
                    Timeout = 5000,
                    EnableSsl = isSsl
                });

                config.AddRule(NLog.LogLevel.FromOrdinal(minMailLogLevel.ConvertTo<int>()), NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotMail);
            }

            // Add loggers
            builder.AddConsole();
            builder.AddNLog(config);
        }

        private static bool HasStringValue(string value)
        {
            return value.HasValue();
        }

        private static string CreateConfigValueEmptyMessage(string name, string value)
        {
            return $"{name} cannot be empty or whitespace. Was <{value}>";
        }

        private static FileTarget CreateLogFileTarget(string targetName, DirectoryInfo logDirectory, FileSize archiveSize, string layout = PlotBotConstants.Logging.Layout)
        {
            return new FileTarget()
            {
                Name = targetName,
                Layout = layout,
                FileName = Path.Combine(logDirectory.FullName, $"{targetName}.txt"),
                ArchiveFileName = Path.Combine(logDirectory.FullName, PlotBotConstants.Logging.ArchiveFolder, $"{targetName}_{{###}}.txt"),
                ArchiveAboveSize = archiveSize.ByteSize,
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ConcurrentWrites = false
            };
        }
    }
}
