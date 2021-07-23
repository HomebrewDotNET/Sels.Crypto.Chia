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
        /// Unique name to identifiy this plotter config.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Size of plots that this plotter will create.
        /// </summary>
        public string PlotSize { get; set; } = PlotBotConstants.Settings.Plotter.DefaultPlotSize;
        /// <summary>
        /// How many instances this plotter can use.
        /// </summary>
        public int MaxInstances { get; set; } = PlotBotConstants.Settings.Plotter.DefaultInstances;
        /// <summary>
        /// Total amount of thread this plotter can use. Threads are divided between all instances.
        /// </summary>
        public int TotalThreads { get; set; } = PlotBotConstants.Settings.Plotter.DefaultThreads;
        /// <summary>
        /// Total amount fo ram this plotter can use. Ram is divided between all instances.
        /// </summary>
        public int TotalRam { get; set; } = PlotBotConstants.Settings.Plotter.DefaultRam;
        /// <summary>
        /// How many buckets this plotter uses to plot.
        /// </summary>
        public int Buckets { get; set; } = PlotBotConstants.Settings.Plotter.DefaultBuckets;

        /// <summary>
        /// Contains settings about which directories the plotter can use to plot.
        /// </summary>
        public PlotterWorkingConfig WorkingDirectories { get; set; }

        /// <summary>
        /// Contains config on when a new instance is allowed to plot to a drive.
        /// </summary>
        public PlotterDelayConfig[] DelaySettings { get; set; }
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
        public bool ArchiveProgressFiles { get; set; } = PlotBotConstants.Settings.Plotter.Directory.DefaultArchiveProgressFiles;
    }

    /// <summary>
    /// Contains config on when a new instance is allowed to plot to a drive.
    /// </summary>
    public class PlotterDelayConfig
    {
        /// <summary>
        /// Name of the component that checks if an instance is allowed to run.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Arguments for the delay component.
        /// </summary>
        public string[] Arguments { get; set; }
    }
}
