using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot
{
    public static class PlotBotConstants
    {
        public const string ServiceName = "Plot Bot";
        public const string LoggerName = "PlotBot";

        public static class Plotting
        {
            public const string PlotFileExtension = ".plot";
        }

        public static class Logging
        {
            public const string Layout = "${machinename}|${longdate}|${level:uppercase=true}|${logger}|${message} ${exception}";
            public const string FullLayout = "${machinename}|${longdate}|${level:uppercase=true}|${logger}|${message} ${newline}${exception:format=tostring}";
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
                public const string DevMode = "PlotBot.Service.DevMode";
                public const string TestMode = "PlotBot.Service.TestMode";
                public const string ConfigFile = "PlotBot.Service.ConfigFile";
                public const string Interval = "PlotBot.Service.Interval";
                public const string MinLogLevel = "PlotBot.Logging.MinLevel";
                public const string LogDirectory = "PlotBot.Logging.Directory";
                public const string ArchiveSize = "PlotBot.Logging.ArchiveSize";
            }
        }

        public static class Settings
        {
            public static string DefaultCommand = $"/opt/chia-plotter/build/chia_plot -n 1 -r {Parameters.Threads} -u {Parameters.Buckets} -t {Parameters.CacheOne}/ -2 {Parameters.CacheTwo}/ -d {Parameters.Destination}/ -f {Parameters.FarmerKey} -c {Parameters.PoolContractAddress} -w";

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
            public static class PlotFileNameSeeker
            {
                public const string String = "String";
                public const string StringFilter = "Filter";
                public const string StringFilterArg = "plot-";

                public const string Regex = "Regex";
            }

            public static class Clearer
            {
                public const string OgDate = "OgDate";
                public const string OgDateThreshold = "Threshold";
                public const string OgDateThresholdArg = "07/07/2021";

            }

            public static class Delay
            {
                public const string TimeStarted = "TimeStarted";
                public const string TimeStartedDelay = "MinuteDelay";
                public const int TimeStartedDelayArg = 60;

                public const string ProgressFileContains = "ProgressFile";
                public const string ProgressFileContainsFilter = "Filter";
                public const string ProgressFileContainsFilterDefaultArg = "Phase 1 took";
            }
        }

        public static class Parameters
        {
            public static class Names
            {
                public const string PlotterAlias = "PlotterAlias";
                public const string PlotterInstance = "PlotterInstance";
                public const string DriveAlias = "DriveAlias";

                public const string PlotSize = "PlotSize";
                public const string Threads = "Threads";
                public const string Buckets = "Buckets";
                public const string Ram = "Ram";
                public const string Destination = "$Destination";
                public const string PoolKey = "PoolKey";
                public const string PoolContractAddress = "PoolContractAddress";
                public const string FarmerKey = "FarmerKey";

                public const string Cache = "$Cache";
            }

            public const string PlotterAlias = "${{" + Names.PlotterAlias + "}}";
            public const string PlotterInstance = "${{" + Names.PlotterInstance + "}}";
            public const string DriveAlias = "${{" + Names.DriveAlias + "}}";

            public const string PlotSize = "${{" + Names.PlotSize + "}}";
            public const string Threads = "${{" + Names.Threads + "}}";
            public const string Buckets = "${{" + Names.Buckets + "}}";
            public const string Ram = "${{" + Names.Ram + "}}";
            public const string Destination = "${{" + Names.Destination + "}}";
            public const string PoolKey = "${{" + Names.PoolKey + "}}";
            public const string PoolContractAddress = "${{" + Names.PoolContractAddress + "}}";
            public const string FarmerKey = "${{" + Names.FarmerKey + "}}";

            
            public const string CacheOne = "${{" + Names.Cache + "_1}}";
            public const string CacheTwo = "${{" + Names.Cache + "_2}}";
        }

    }
}
