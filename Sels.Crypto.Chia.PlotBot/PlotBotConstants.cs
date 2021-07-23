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
            public const string Layout = "${machinename}|${longdate}|${level:uppercase=true}|${logger}|${message} ${newline}${exception}";
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
            public static class Plotter
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

            public static class Drive
            {
                public const int DefaultPriority = 1;
            }
        }
    }
}
