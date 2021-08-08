using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models.Config
{
    /// <summary>
    /// Contains settings for a plotter.
    /// </summary>
    public class PlotterConfig
    {
        /// <summary>
        /// Boolean indicating if this plotter is allowed to start new instances.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Unique name to identifiy this plotter config.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Size of plots that this plotter will create.
        /// </summary>
        public string PlotSize { get; set; } = PlotBotConstants.Settings.Plotters.DefaultPlotSize;
        /// <summary>
        /// How many instances this plotter can use.
        /// </summary>
        public int MaxInstances { get; set; } = PlotBotConstants.Settings.Plotters.DefaultInstances;
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

        /// <summary>
        /// Component that searches for the plot file name when an instance is plotting.
        /// </summary>
        public ComponentConfig PlotFileNameSeeker { get; set; }

        /// <summary>
        /// Contains settings about which directories the plotter can use to plot.
        /// </summary>
        public PlotterWorkingConfig WorkingDirectories { get; set; }

        /// <summary>
        /// Contains config on when a new instance is allowed to plot to a drive.
        /// </summary>
        public ComponentConfig[] DelaySettings { get; set; }

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotterConfig Default => new PlotterConfig()
        {
            Alias = "MainPlotter",
            PlotFileNameSeeker = new ComponentConfig()
            {
                Name = PlotBotConstants.Components.PlotFileNameSeeker.String,
                Arguments = new Dictionary<string, string>()
                {
                    { PlotBotConstants.Components.PlotFileNameSeeker.StringFilter, PlotBotConstants.Components.PlotFileNameSeeker.StringFilterArg }
                }
            },
            WorkingDirectories = PlotterWorkingConfig.Default,
            DelaySettings = new ComponentConfig[] { ComponentConfig.DefaultPlotterDelayer }
        };
    }

    /// <summary>
    /// Contains settings about which directories the plotter can use to plot.
    /// </summary>
    public class PlotterWorkingConfig
    {
        /// <summary>
        /// List of cache directories the plotter can use to create plots
        /// </summary>
        public string[] Caches { get; set; }
        /// <summary>
        /// Directory the plotter uses as working directory. This is where the progress files are placed to monitor the progress of the plotter.
        /// </summary>
        public string WorkingDirectory { get; set; }
        /// <summary>
        /// If we should archive progress files once a plot instance is done plotting. Can be handy to keep a history.
        /// </summary>
        public bool ArchiveProgressFiles { get; set; } = PlotBotConstants.Settings.Plotters.Directory.DefaultArchiveProgressFiles;

        // Statics
        /// <summary>
        /// Default instance.
        /// </summary>
        public static PlotterWorkingConfig Default => new PlotterWorkingConfig()
        {
            Caches = new string[] { "/path/to/cache/one", "/path/to/cache/two" },
            WorkingDirectory = "/path/to/plotter/working/directory"
        };
    }
}
