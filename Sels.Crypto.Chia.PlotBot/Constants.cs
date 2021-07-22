using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot
{
    public static class Constants
    {
        public static class Configuration
        {

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
