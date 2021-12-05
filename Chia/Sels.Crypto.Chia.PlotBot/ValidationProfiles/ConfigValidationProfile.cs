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
using Sels.ObjectValidationFramework.Templates.Profile;

namespace Sels.Crypto.Chia.PlotBot.ValidationProfiles
{
    public class ConfigValidationProfile : ValidationProfile<string>, IPlotBotConfigValidator
    {
        // Constants
        private const string PlotBotName = PlotBotConstants.ServiceName;

        public ConfigValidationProfile(IServiceFactory factory)
        {
            CreateValidationFor<PlotBotConfig>()
                .ForProperty(x => x.Settings).CannotBeNull(x => $"{x.GetDisplayName()} section must be defined")
                .ForProperty(x => x.Plotters).MustContainAtLeast(1, x => $"{x.GetDisplayName()} must contain at least {1} Plotter")
                .ForProperty(x => x.Plotters, x => x.Select(p => p.Alias)).AllMustBeUnique(x => $"Plotter {nameof(PlotterConfig.Alias)} must be unique between all plotters.")
                .ForProperty(x => x.Drives, x => x.Select(d => d.Alias)).AllMustBeUnique(x => $"Drive {nameof(DriveConfig.Alias)} must be unique between all drives.")
                .ForProperty(x => x.Plotters).InvalidIf(x => x.Value.Any(p => !x.Source.Settings.PlotSizes.Select(x => x.Name).Contains(p.Command.PlotSize)), x => $"Plotter contained a {nameof(PlotterCommandConfig.PlotSize)} that wasn't defined in {nameof(x.Source.Settings)}.{nameof(x.Source.Settings.PlotSizes)}");

            CreateValidationFor<PlotBotSettingsConfig>()
                .ForSource().InvalidIf(x => !x.Value.PoolKey.HasValue() && !x.Value.PoolContractAddress.HasValue(), x => $"Either {nameof(x.Value.PoolKey)} or {nameof(x.Value.PoolContractAddress)} needs to be defined")
                .ForProperty(x => x.FarmerKey).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.DefaultPlotCommand).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.PlotSizes).MustContainAtLeast(1, x => $"{x.GetDisplayName()} must contain at least 1 Plot Size")
                .ForElements(x => x.DriveClearers).ValidIf(x => factory.IsRegistered<IDriveSpaceClearer>(x.Value.Name), x => $"{x.GetDisplayName()} is not a known clearer. Was <{x.Value}>");

            CreateValidationFor<PlotSizeConfig>()
                .ForProperty(x => x.Name).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.CreationSize).MustBeLargerThan(0, x => $"{x.GetDisplayName()} must be larger than 0. Was <{x.Value}>")
                .ForProperty(x => x.FinalSize).MustBeLargerThan(0, x => $"{x.GetDisplayName()} must be larger than 0. Was <{x.Value}>");

            CreateValidationFor<PlotterCommandConfig>()
                .ForProperty(x => x.PlotSize).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.TotalThreads).MustBeLargerOrEqualTo(1, x => $"{x.GetDisplayName()} must be equal or above 1")
                .ForProperty(x => x.TotalRam).MustBeLargerOrEqualTo(1000, x => $"{x.GetDisplayName()} must be equal or above 1000")
                .ForProperty(x => x.Buckets).MustBeLargerOrEqualTo(1, x => $"{x.GetDisplayName()} must be equal or above 1");

            CreateValidationFor<PlotterConfig>()
                .ForProperty(x => x.Alias).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.MaxInstances).ValidIf(x => x!= null && x.Value >= 1, x => $"{x.GetDisplayName()} must be equal or above 1")
                .ForProperty(x => x.Progress).CannotBeNull(x => $"{x.GetDisplayName()} section must be defined")
                .ForProperty(x => x.Work).CannotBeNull(x => $"{x.GetDisplayName()} section must be defined")
                .ForProperty(x => x.Progress).ValidIf(x => factory.IsRegistered<IPlotProgressParser>(x.Value.Name), x => $"{x.GetDisplayName()} is not a known file name seeker. Was <{x.Value}>")
                .ForElements(x => x.Delay).ValidIf(x => factory.IsRegistered<IPlotterDelayer>(x.Value.Name), x => $"{x.GetDisplayName()} is not a known delayer. Was <{x.Value}>");

            CreateValidationFor<PlotterWorkingConfig>()
                .ForProperty(x => x.Caches).CannotBeEmpty(x => $"{x.GetDisplayName()} must contain at least 1 cache ")
                .ForProperty(x => x.WorkingDirectory).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.WorkingDirectory).MustBeValidPath(x => $"{x.GetDisplayName()} must be valid directory. Was <{x.Value}>");

            CreateValidationFor<PlotterCacheConfig>()
                .ForProperty(x => x.Directory).MustBeExistingPath(x => $"{x.GetDisplayName()} cannot be empty or whitespace and the directory must exist. Was <{x.Value}>")
                .ForProperty(x => x.Distribution).MustBeLargerThan(0, x => $"{x.GetDisplayName()} must be larger than 0. Was <{x.Value}>");

            CreateValidationFor<SharedConfig>()
                .ForProperty(x => x.Alias).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.Timeout).ValidIf(x => !x.Value.HasValue || x.Value > 0, x => $"{x.GetDisplayName()} must be above 0. Was <{x.Value}>");

            CreateValidationFor<ComponentConfig>()
                .ForProperty(x => x.Name).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} cannot be empty or whitespace. Was <{x.Value}>")
                .ForProperty(x => x.Arguments).CannotBeNull(x => $"{x.GetDisplayName()} must be defined")
                .ForElements(x => x.Arguments, x => x.Key).CannotBeNullOrWhitespace(x => $"{x.GetDisplayName()} key cannot be empty or whitespace. Was <{x.Value}>");

            CreateValidationFor<DriveConfig>()
                .ForProperty(x => x.Directory).MustBeExistingPath(x => $"{x.GetDisplayName()} must be valid directory. Was <{x.Value}>");
        }

        public IEnumerable<string> Validate(PlotBotConfig config)
        {
            using var logger = LoggingServices.TraceMethod(this);
            return Validate(config, default);
        }
    }
}
