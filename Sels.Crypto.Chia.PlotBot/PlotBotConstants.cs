using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot
{
    public static class PlotBotConstants
    {
        public const string ServiceName = "Plot Bot";
        public const string LoggerName = "PlotBot";

        public static class Logging
        {
            public const string Layout = "${machinename}|${longdate}|${level:uppercase=true}|${logger}|${message} ${exception}";
            public const string ArchiveFolder = "Archive";

            public static class Categories
            {
                public const string Microsoft = "Microsoft.*";
            }

            public static class Targets
            {
                public const string PlotBotAll = "PlotBot_All";
                public const string PlotBotError = "PlotBot_Error";
                public const string PlotBotCritical = "PlotBot_Critical";
            }
        }

        public static class Config
        {
            public static class AppSettings
            {
                public const string ConfigFile = "PlotBot.Service.ConfigFile";
                public const string Interval = "PlotBot.Service.Interval";
                public const string MinLogLevel = "PlotBot.Logging.MinLevel";
                public const string LogDirectory = "PlotBot.Logging.Directory";
                public const string ArchiveSize = "PlotBot.Logging.ArchiveSize";
            }
        }

        public static class Settings
        {
            public static string DefaultCommand = $"/opt/chia-plotter/build/chia_plot -n 1 -r {Parameters.Threads} -u {Parameters.Buckets} -t {Parameters.CacheOne}/{Parameters.DriveAlias}/{Parameters.PlotterAlias}/{Parameters.PlotterInstance} -2 {Parameters.CacheTwo}/{Parameters.DriveAlias}/{Parameters.PlotterAlias}/{Parameters.PlotterInstance} -d {Parameters.Destination} -p {Parameters.PoolKey} -f {Parameters.FarmerKey} -c {Parameters.PoolContractAddress} -w";

            public static class Plotters
            {
                public const string DefaultPlotSize = "K32";
                public const int DefaultInstances = 1;
                public const int DefaultThreads = 4;
                public const int DefaultRam = 4000;
                public const int DefaultBuckets = 128;

                public static class Directory
                {
                    public const bool DefaultArchiveProgressFiles = true;
                }
            }

            public static class Drives
            {
                public const int DefaultPriority = 1;
            }
        }

        public static class Components
        {
            public static class Delay
            {
                public const string TimeStarted = "TimeStarted";
                public const int TimeStartedDefaultArg = 60;

                public const string ProgressFileContains = "ProgressFileContains";
                public const string ProgressFileContainsDefaultArg = "Phase 1 took";
            }
        }

        public static class Parameters
        {
            public const string PlotterAlias = "${PlotterAlias}";
            public const string PlotterInstance = "${PlotterInstance}";
            public const string DriveAlias = "${PlotterAlias}";

            public const string PlotSize = "${PlotSize}";
            public const string Threads = "${Threads}";
            public const string Buckets = "${Buckets}";
            public const string Ram = "${Ram}";
            public const string Destination = "Destination";
            public const string PoolKey = "${PoolKey}";
            public const string PoolContractAddress = "${PoolContractAddress}";
            public const string FarmerKey = "${FarmerKey}";

            public const string CacheOne = "${Cache_1}";
            public const string CacheTwo = "${Cache_2}";
        }
    }
}
