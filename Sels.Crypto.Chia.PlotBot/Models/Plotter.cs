using Sels.Core.Components.FileSystem;
using Sels.Crypto.Chia.PlotBot.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Models
{
    public class Plotter
    {
        // Fields

        // Properties
        #region PlotSettings
        /// <summary>
        /// Public pool key.
        /// </summary>
        public string PoolKey { get; set; }
        /// <summary>
        /// Pool contract address.
        /// </summary>
        public string PoolContractAddress { get; set; }
        /// <summary>
        /// Public farmer key.
        /// </summary>
        public string FarmerKey { get; set; }
        #endregion

        #region PlotterSettings
        /// <summary>
        /// Unique name to identifiy this plotter config.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Size of plots that this plotter will create.
        /// </summary>
        public PlotSize PlotSize { get; set; }
        /// <summary>
        /// How many instances this plotter can use.
        /// </summary>
        public int MaxInstances { get; set; }
        /// <summary>
        /// Total amount of thread this plotter can use. Threads are divided between all instances.
        /// </summary>
        public int TotalThreads { get; set; }
        /// <summary>
        /// Total amount fo ram this plotter can use. Ram is divided between all instances.
        /// </summary>
        public int TotalRam { get; set; }
        /// <summary>
        /// How many buckets this plotter uses to plot.
        /// </summary>
        public int Buckets { get; set; }
        /// <summary>
        /// Command that starts a new process that creates plots.
        /// </summary>
        public string PlotCommand { get; set; }
        #endregion

        #region PlotterWorkingSettings
        /// <summary>
        /// List of cache directories the plotter can use to create plots
        /// </summary>
        public CrossPlatformDirectory[] Caches { get; set; }
        /// <summary>
        /// Directory the plotter uses as working directory. This is where the progress files are placed to monitor the progress of the plotter.
        /// </summary>
        public CrossPlatformDirectory WorkingDirectory { get; set; }
        /// <summary>
        /// If we should archive progress files once a plot instance is done plotting. Can be handy to keep a history.
        /// </summary>
        public bool ArchiveProgressFiles { get; set; }
        #endregion

        /// <summary>
        /// Checks if plotter is allowed to plot to a certain drive
        /// </summary>
        public IPlotterDelayer[] PlotterDelayers { get; set; }

        /// <summary>
        /// Indicates if this plotter has any active running instances.
        /// </summary>
        public bool HasRunningInstances { get; set; }
        /// <summary>
        /// Plotters tagged for deletion can't start new instances.
        /// </summary>
        public bool TaggedForDeletion { get; set; }

    }
}
