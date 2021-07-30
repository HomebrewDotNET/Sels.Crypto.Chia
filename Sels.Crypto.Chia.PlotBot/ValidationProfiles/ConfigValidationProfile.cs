using Sels.Crypto.Chia.PlotBot.Models.Config;
using Sels.ObjectValidationFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sels.Core.Extensions;
using Sels.Crypto.Chia.PlotBot.Contracts;

namespace Sels.Crypto.Chia.PlotBot.ValidationProfiles
{
    public class ConfigValidationProfile : ValidationProfile<string>, IPlotBotConfigValidator
    {
        // Constants
        private const string PlotBotName = PlotBotConstants.ServiceName;

        public ConfigValidationProfile()
        {
            CreateValidator<PlotBotConfig>()
                .IfNull(() => $"{PlotBotName} configuration cannot be empty or whitespace")
                .CannotBeNull(x => x.Settings, x => $"{x.Property.Name} section must be defined")
                .MustContainAtLeast(x => x.Plotters, 1, x => $"{x.Property.Name} must contain at least {1} Plotter")
                .AllElementsMustBeUnique(x => x.Plotters, x => x.Alias, x => $"Plotter {nameof(PlotterConfig.Alias)} must be unique between all plotters. Was <{x.PropertyValue}>")
                .AllElementsMustBeUnique(x => x.Drives, x => x.Alias, x => $"Plotter {nameof(DriveConfig.Alias)} must be unique between all drives. Was <{x.PropertyValue}>")
                .AddInvalidValidation(x => x.Plotters.Any(p => !x.Settings.PlotSizes.Select(x => x.Name).Contains(p.PlotSize)), x => $"Plotter contained a {nameof(PlotterConfig.PlotSize)} that wasn't defined in {nameof(x.Settings)}.{nameof(x.Settings.PlotSizes)}");

            CreateValidator<PlotBotSettingsConfig>()
                .AddInvalidValidation(x => !x.PoolKey.HasValue() && !x.PoolContractAddress.HasValue(), x => $"Either {nameof(x.PoolKey)} or {nameof(x.PoolContractAddress)} needs to be defined")
                .CannotBeNullOrWhiteSpace(x => x.FarmerKey, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .CannotBeNullOrWhiteSpace(x => x.DefaultPlotCommand, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .MustContainAtLeast(x => x.PlotSizes, 1, x => $"{x.Property.Name} must contain at least {1} Plot Size");

            CreateValidator<PlotSizeConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Name, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .MustBePositive(x => x.CreationSize, x => $"{x.Property.Name} must be larger than 0. Was <{x.PropertyValue}>")
                .MustBePositive(x => x.FinalSize, x => $"{x.Property.Name} must be larger than 0. Was <{x.PropertyValue}>");

            CreateValidator<PlotterConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Alias, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .CannotBeNullOrWhiteSpace(x => x.PlotSize, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .AddValidValidation(x => x.MaxInstances, x => x >= 1, x => $"{x.Property.Name} must be equal or above 1")
                .AddValidValidation(x => x.TotalThreads, x => x >= 1, x => $"{x.Property.Name} must be equal or above 1")
                .AddValidValidation(x => x.TotalRam, x => x >= 1000, x => $"{x.Property.Name} must be equal or above 1000")
                .AddValidValidation(x => x.Buckets, x => x >= 1, x => $"{x.Property.Name} must be equal or above 1")
                .CannotBeNull(x => x.WorkingDirectories, x => $"{x.Property.Name} section must be defined");

            CreateValidator<PlotterWorkingConfig>()
                .CannotBeEmpty(x => x.Caches, x => $"{x.Property.Name} must contain at least 1 directory")
                .AddValidCollectionValidation(x => x.Caches, x => x.HasValue() && Directory.Exists(x), x => $"Cache directory cannot be empty or whitespace and the directory must exist. Was <{x.ElementValue}>")
                .CannotBeNullOrWhiteSpace(x => x.WorkingDirectory, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .IsValidDirectory(x => x.WorkingDirectory, x => $"{x.Property.Name} must be valid directory. Was <{x.PropertyValue}>");

            CreateValidator<PlotterDelayConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Name, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .AddValidCollectionValidation(x => x.Arguments, x => x.HasValue(), x => $"Argument cannot be empty or whitespace. Was <{x.ElementValue}>");

            CreateValidator<DriveConfig>()
                .CannotBeNullOrWhiteSpace(x => x.Alias, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .CannotBeNullOrWhiteSpace(x => x.Directory, x => $"{x.Property.Name} cannot be empty or whitespace. Was <{x.PropertyValue}>")
                .IsValidDirectory(x => x.Directory, x => $"{x.Property.Name} must be valid directory. Was <{x.PropertyValue}>");
        }

        public IEnumerable<string> Validate(PlotBotConfig config)
        {
            return ObjectValidator.Validate(this, config, typeof(PlotBotConfig));
        }
    }
}
