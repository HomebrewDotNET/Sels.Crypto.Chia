using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models.Config
{
    /// <summary>
    /// Contains settings for a plotter.
    /// </summary>
    public class PlotterConfig : SharedConfig
    {
        /// <summary>
        /// Contains settings for the plot command.
        /// </summary>
        public PlotterCommandConfig Command { get; set; }

        /// <summary>
        /// Component that extracts information from the progress file.
        /// </summary>
        public ComponentConfig Progress { get; set; }

        /// <summary>
        /// Contains settings about which directories the plotter can use to plot.
        /// </summary>
        public PlotterWorkingConfig Work { get; set; }

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotterConfig Default => new PlotterConfig()
        {
            Alias = "MainPlotter",
            Command = PlotterCommandConfig.Default,
            MaxInstances = PlotBotConstants.Settings.Plotters.DefaultInstances,
            Progress = new ComponentConfig()
            {
                Name = PlotBotConstants.Components.PlotProgressParser.String,
                Arguments = new Dictionary<string, string>()
                {
                    { PlotBotConstants.Components.PlotProgressParser.StringFilter, PlotBotConstants.Components.PlotProgressParser.StringFilterArg },
                    { PlotBotConstants.Components.PlotProgressParser.StringTransferExtension, PlotBotConstants.Components.PlotProgressParser.StringTransferExtensionArg }
                }
            },
            Work = PlotterWorkingConfig.Default,
            Delay = new ComponentConfig[] { ComponentConfig.DefaultPlotterDelayer }
        };
    }

    /// <summary>
    /// Contains settings about which directories the plotter can use to plot and ones to modify the plotter behaviour.
    /// </summary>
    public class PlotterWorkingConfig
    {
        /// <summary>
        /// List of cache directories the plotter can use to create plots
        /// </summary>
        public PlotterCacheConfig[] Caches { get; set; }
        /// <summary>
        /// Directory the plotter uses as working directory. This is where the progress files are placed to monitor the progress of the plotter.
        /// </summary>
        public string WorkingDirectory { get; set; }
        /// <summary>
        /// If we should archive progress files once a plot instance is done plotting. Can be handy to keep a history.
        /// </summary>
        public bool ArchiveProgressFiles { get; set; } = PlotBotConstants.Settings.Plotters.Work.DefaultArchiveProgressFiles;
        /// <summary>
        /// If plot bot should throw an exception when it is missing free space on a cache directory.
        /// </summary>
        public bool ThrowOnMissingCacheSpace { get; set; } = PlotBotConstants.Settings.Plotters.Work.DefaultThrowOnMissingCacheSpace;

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotterWorkingConfig Default => new PlotterWorkingConfig()
        {
            Caches = PlotterCacheConfig.Default,
            WorkingDirectory = "/path/to/plotter/working/directory"
        };
    }

    public class PlotterCacheConfig
    {
        /// <summary>
        /// Cache root directory.
        /// </summary>
        public string Directory { get; set; }
        /// <summary>
        /// Percentage of size used defined by <see cref="PlotSizeConfig.CreationSize"/>.
        /// </summary>
        public double Distribution { get; set; }

        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotterCacheConfig[] Default => new PlotterCacheConfig[]
        {
            new PlotterCacheConfig
            {
                Directory = "/path/to/cache/",
                Distribution = 1
            },
            new PlotterCacheConfig
            {
                Directory = "/path/to/temp/cache/",
                Distribution = 1
            }
        };
    }

    /// <summary>
    /// Contains settings for the plot command.
    /// </summary>
    public class PlotterCommandConfig
    {
        /// <summary>
        /// Size of plots that this plotter will create.
        /// </summary>
        public string PlotSize { get; set; } = PlotBotConstants.Settings.Plotters.DefaultPlotSize;
        /// <summary>
        /// Total amount of thread this plotter can use. Threads are divided between all instances.
        /// </summary>
        public int TotalThreads { get; set; } = PlotBotConstants.Settings.Plotters.DefaultThreads;
        /// <summary>
        /// Total amount fo ram this plotter can use. Ram is divided between all instances.
        /// </summary>
        public int TotalRam { get; set; } = PlotBotConstants.Settings.Plotters.DefaultRam;
        /// <summary>
        /// How many buckets this plotter uses to plot.
        /// </summary>
        public int Buckets { get; set; } = PlotBotConstants.Settings.Plotters.DefaultBuckets;
        /// <summary>
        /// Command that starts a new process that creates plots. If left empty the <see cref="PlotBotSettingsConfig.DefaultPlotCommand"/> will be used.
        /// </summary>
        public string PlotCommand { get; set; }

        public static PlotterCommandConfig Default => new PlotterCommandConfig();
    }
}
