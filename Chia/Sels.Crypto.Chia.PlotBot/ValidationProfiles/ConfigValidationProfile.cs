using Sels.Crypto.Chia.PlotBot.Models.Config;
using Sels.ObjectValidationFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sels.Core.Extensions;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Core.Contracts.Configuration;
using Sels.Core;
using Sels.Core.Contracts.Factory;
using Sels.Core.Components.Logging;

namespace Sels.Crypto.Chia.PlotBot.ValidationProfiles
{
    public class ConfigValidationProfile : ValidationProfile<string>, IPlotBotConfigValidator
    {
        // Constants
        private const string PlotBotName = PlotBotConstants.ServiceName;

        public ConfigValidationProfile(IServiceFactory factory)
        {
            CreateValidator<PlotBotConfig>()
                .IfNull(() => $"{PlotBotName} configuration cannot be empty or whitespace")
                .CannotBeNull(x => x.Settings, x => $"{x.Property.Name} section must be defined")
                .MustContainAtLeast(x => x.Plotters, 1, x => $"{x.Property.Name} must contain at least {1} Plotter")
                .AllElementsMustBeUnique(x => x.Plotters, x => x.Alias, x => $"Plotter {nameof(PlotterConfig.Alias)} must be unique between all plotters.")
                .AllElementsMustBeUnique(x => x.Drives, x => x.Alias, x => $"Drive {nameof(DriveConfig.Alias)} must be unique between all drives.")
                .AddInvalidValidation(x => x.Plotters.Any(p => !x.Settings.PlotSizes.Select(x => x.Name).Contains(p.Command.PlotSize)), x => $"Plotter contained a {nameof(PlotterCommandConfig.PlotSize)} that wasn't defined in {nameof(x.Settings)}.{nameof(x.Settings.PlotSizes)}");

            CreateValidator<PlotBotSettingsConfig>()
                .AddInvalidValidation(x => !x.PoolKey.HasValue() && !x.PoolContractAddress.HasValue(), x => $"Either {nameof(x.PoolKey)} or {nameof(x.PoolContractAddress)} needs to be defined")
                .CannotBeNullOrWhiteSpace(x => x.FarmerKey, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .CannotBeNullOrWhiteSpace(x => x.DefaultPlotCommand, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .MustContainAtLeast(x => x.PlotSizes, 1, x => $"{x.Property.Name} must contain at least 1 Plot Size")
                .AddValidCollectionValidation(x => x.DriveClearers, x => factory.IsRegistered<IDriveSpaceClearer>(x.Name), x => $"{x.Property.Name} is not a known clearer. Was <{x.ElementValue}>");

            CreateValidator<PlotSizeConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Name, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .MustBePositive(x => x.CreationSize, x => $"{x.Property.Name} must be larger than 0. Was <{x.PropertyValue}>")
                .MustBePositive(x => x.FinalSize, x => $"{x.Property.Name} must be larger than 0. Was <{x.PropertyValue}>");

            CreateValidator<PlotterCommandConfig>()
                .CannotBeNullOrWhiteSpace(x => x.PlotSize, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .AddValidValidation(x => x.TotalThreads, x => x >= 1, x => $"{x.Property.Name} must be equal or above 1")
                .AddValidValidation(x => x.TotalRam, x => x >= 1000, x => $"{x.Property.Name} must be equal or above 1000")
                .AddValidValidation(x => x.Buckets, x => x >= 1, x => $"{x.Property.Name} must be equal or above 1");

            CreateValidator<PlotterConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Alias, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .AddValidValidation(x => x.MaxInstances, x => x >= 1, x => $"{x.Property.Name} must be equal or above 1")
                .CannotBeNull(x => x.Progress, x => $"{x.Property.Name} section must be defined")
                .CannotBeNull(x => x.Work, x => $"{x.Property.Name} section must be defined")
                .AddValidValidation(x => x.Progress, x => factory.IsRegistered<IPlotProgressParser>(x.Name), x => $"{x.Property.Name} is not a known file name seeker. Was <{x.PropertyValue}>")
                .AddValidCollectionValidation(x => x.Delay, x => factory.IsRegistered<IPlotterDelayer>(x.Name), x => $"{x.Property.Name} is not a known delayer. Was <{x.ElementValue}>");

            CreateValidator<PlotterWorkingConfig>()
                .CannotBeEmpty(x => x.Caches, x => $"{x.Property.Name} must contain at least 1 cache ")
                .CannotBeNullOrWhiteSpace(x => x.WorkingDirectory, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .IsValidDirectory(x => x.WorkingDirectory, x => $"{x.Property.Name} must be valid directory. Was <{x.PropertyValue}>");

            CreateValidator<PlotterCacheConfig>()
                .AddValidValidation(x => x.Directory, x => x.HasValue() && Directory.Exists(x), x => $"Cache directory cannot be empty or whitespace and the directory must exist. Was <{x.PropertyValue}>")
                .AddValidValidation(x => x.Distribution, x => x > 0, x => $"Cache distribution must be larger than 0. Was <{x.PropertyValue}>");

            CreateValidator<SharedConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Alias, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .AddValidValidation(x => x.Timeout, x => !x.HasValue || x.Value > 0, x => $"{x.Property.Name} must be above 0. Was <{x.PropertyValue}>");

            CreateValidator<ComponentConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Name, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>") 
                .CannotBeNull(x => x.Arguments, x => $"{x.Property.Name} must be defined")
                .AddValidCollectionValidation(x => x.Arguments, x => x.Key.HasValue(), x => $"Argument key cannot be empty or whitespace. Was <{x.ElementValue}>");

            CreateValidator<DriveConfig>()                
                .CannotBeNullOrWhiteSpace(x => x.Directory, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .IsValidDirectory(x => x.Directory, x => $"{x.Property.Name} must be valid directory. Was <{x.PropertyValue}>");
        }

        public IEnumerable<string> Validate(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);
            return ObjectValidator.Validate(this, config, typeof(PlotBotConfig));
        }
    }
}
