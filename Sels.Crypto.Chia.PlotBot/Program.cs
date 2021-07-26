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

namespace Sels.Crypto.Chia.PlotBot
{
    public class Program
    {
        // Constants
        private const string ConfigProviderKey = "ConfigProvider";

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services
                    services.AddHostedService<PlotBotManager>();
                    services.AddSingleton<PlotBot>();
                    services.AddSingleton<IConfigProvider, ConfigProvider>();
                    services.AddSingleton<IPlotBotConfigValidator, ConfigValidationProfile>();
                    services.AddSingleton<IFactory<CrossPlatformDirectory>, LinuxDirectoryFactory>();
                    services.AddSingleton<IObjectFactory, AliasTypeFactory>(x => {
                        return new AliasTypeFactory(x.GetRequiredService<IConfigProvider>(), GenericConverter.DefaultConverter);
                    });


                    // Setup service factory
                    services.AddSingleton<IServiceFactory, UnityServiceFactory>(x => {
                        var factory = new UnityServiceFactory();
                        factory.LoadFrom(services);
                        return factory;
                    });

                    // Save state
                    var provider = services.BuildServiceProvider();
                    var configProvider = provider.GetRequiredService<IConfigProvider>();

                    hostContext.Properties.Add(ConfigProviderKey, configProvider);
                })
                .ConfigureLogging(SetupLogging);

        private static void SetupLogging(HostBuilderContext context, ILoggingBuilder builder)
        {
            // Read config
            var configProvider = context.Properties[ConfigProviderKey].As<IConfigProvider>();
            var minLogLevel = configProvider.GetAppSetting<LogLevel>(PlotBotConstants.Config.AppSettings.MinLogLevel);
            var logDirectory = configProvider.GetAppSetting(PlotBotConstants.Config.AppSettings.LogDirectory, true, x => x.HasValue() && Directory.Exists(x), x => $"Directory cannot be empty and Directory must exist on the file system. Was <{x}>");
            var archiveSize = configProvider.GetAppSetting<long>(PlotBotConstants.Config.AppSettings.ArchiveSize, true, x => x > 1, x => $"File size cannot be empty and file size must be above 1 {MegaByte.FileSizeAbbreviation}");
            var archiveFileSize = FileSize.CreateFromSize<MegaByte>(archiveSize);

            var logDirectoryInfo = new DirectoryInfo(logDirectory);
            var minLogLevelOrdinal = minLogLevel.ConvertTo<int>();

            // Clear providers and set basic settings
            builder.ClearProviders();
            builder.SetMinimumLevel(minLogLevel);

            var config = new LoggingConfiguration();

            // Create targets
            config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotAll, logDirectoryInfo, archiveFileSize));
            config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotError, logDirectoryInfo, archiveFileSize));
            config.AddTarget(CreateLogFileTarget(PlotBotConstants.Logging.Targets.PlotBotCritical, logDirectoryInfo, archiveFileSize));

            // Create rules
            var nlogMinLevel = NLog.LogLevel.FromOrdinal(minLogLevelOrdinal);
            // Skip microsoft logs
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Info, new NullTarget(), PlotBotConstants.Logging.Categories.Microsoft, true);
            // All logs
            config.AddRule(nlogMinLevel, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotAll);
            // All errors
            config.AddRule(minLogLevelOrdinal >= NLog.LogLevel.Warn.Ordinal ? nlogMinLevel : NLog.LogLevel.Warn, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotError);
            // Fatal errors only
            config.AddRule(NLog.LogLevel.Fatal, NLog.LogLevel.Fatal, PlotBotConstants.Logging.Targets.PlotBotCritical);

            // Add loggers
            builder.AddConsole();
            builder.AddNLog(config);
        }

        private static FileTarget CreateLogFileTarget(string targetName, DirectoryInfo logDirectory, FileSize archiveSize)
        {
            return new FileTarget()
            {
                Name = targetName,
                Layout = PlotBotConstants.Logging.Layout,
                FileName = Path.Combine(logDirectory.FullName, $"{targetName}.txt"),
                ArchiveFileName = Path.Combine(logDirectory.FullName, PlotBotConstants.Logging.ArchiveFolder, $"{targetName}_{{###}}.txt"),
                ArchiveAboveSize = archiveSize.ByteSize,
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ConcurrentWrites = false
            };
        }
    }
}
