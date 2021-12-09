using Sels.Core.Components.FileSizes.Byte.Binary;
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
            public static Type DefaultLoggingFileSize = typeof(GibiByte); 
            public const string Layout = "${machinename}|${longdate}|${level:uppercase=true}|${logger}|${message}|${exception}";
            public const string FullLayout = "${machinename}|${longdate}|${level:uppercase=true}|${logger}|${message}: ${newline}${exception:format=ToString}";
            public const string MailSubjectLayout = ServiceName + " ${machinename} ${level:uppercase=true}";
            public const string MailBodyLayout = "Date: ${longdate}${newline}Message: ${message}${newline}Error: ${newline}${exception:format=ToString}";

            public const string ArchiveFolder = "Archive";

            public static class Categories
            {
                public const string Microsoft = "Microsoft.*";
            }

            public static class Targets
            {
                public const string PlotBotDebug = "PlotBot_Debug";
                public const string PlotBotAll = "PlotBot_All";
                public const string PlotBotError = "PlotBot_Error";
                public const string PlotBotCritical = "PlotBot_Critical";
                public const string PlotBotMail = "PlotBot_Mail";
            }
        }

        public static class Config
        {
            public static class AppSettings
            {
                public const string DevMode = "Service.DevMode";
                public const string TestMode = "Service.TestMode";
                public const string ConfigFile = "Service.ConfigFile";
                public const string Interval = "Service.Interval";
                public const string PlottingInterval = "Service.PlottingInterval";
                public const string RetryAfterFailed = "Service.RetryAfterFailed";
                public const string ReduceIdleMessage = "Service.ReduceIdleMessages";

                public const string CleanupCache = "Service.PlotBot.CleanupCache";
                public const string CleanupFailedCopy = "Service.PlotBot.CleanupFailedCopy";
                public const string DriveClearersIdleTime = "Service.PlotBot.DriveClearers.IdleTime";
                public const string ValidatePlotCommand = "Service.PlotBot.ValidatePlotCommand";
            }

            public static class LogSettings
            {
                public const string MinLogLevel = "MinLevel";
                public const string LogDirectory = "Directory";
                public const string ArchiveSize = "ArchiveSize";

                public static class Mail
                {
                    public const string MinLogLevel = "MinLevel";
                    public const string Sender = "Sender";
                    public const string Receivers = "Receivers";
                    public const string Server = "Server";
                    public const string Port = "Port";
                    public const string Username = "Username";
                    public const string Password = "Password";
                    public const string Ssl = "Ssl";
                }
            }
        }

        public static class Settings
        {
            public static string ChiaCommand = $"cd /opt/chia-blockchain && . ./activate && chia plots create -e -f {Parameters.FarmerKey} -c {Parameters.PoolContractAddress} -k {Parameters.PlotSize} -b {Parameters.Ram} -r {Parameters.Threads} -u {Parameters.Buckets} -n {Parameters.PlotAmount} -t {Parameters.CacheTwo}/ -2 {Parameters.CacheOne}/ -d {Parameters.Destination}/";
            public static string MadMaxCommand = $"/opt/chia-plotter/build/chia_plot -n {Parameters.PlotAmount} -r {Parameters.Threads} -u {Parameters.Buckets} -t {Parameters.CacheOne}/ -d {Parameters.Destination}/ -f {Parameters.FarmerKey} -c {Parameters.PoolContractAddress} -w";
            public static string DefaultCommand = MadMaxCommand;

            public static class Plotters
            {
                public const string DefaultPlotSize = "32";
                public const int DefaultInstances = 1;
                public const int DefaultThreads = 4;
                public const int DefaultRam = 4000;
                public const int DefaultBuckets = 128;

                public static class Work
                {
                    public const bool DefaultArchiveProgressFiles = true;
                    public const bool DefaultThrowOnMissingCacheSpace = true;
                }
            }

            public static class Drives
            {
                public const int DefaultPriority = 1;
            }
        }

        public static class Components
        {
            public static class PlotProgressParser
            {
                public const string String = "String";
                public const string StringFilter = "Filter";
                public const string StringFilterArg = "plot-";
                public const string StringTransferExtension = "TransferExtension";
                public const string StringTransferExtensionArg = ".plot.tmp";

                public const string Regex = "Regex";

                public const string MadMax = "MadMax";
                public const string MadMaxFilterArg = "plot-";               
                public const string MadMaxTransferExtensionArg = ".plot.tmp";

                public const string Chia = "Chia";
                public const string ChiaFilterArg = "plot-";
                public const string ChiaTransferExtensionArg = ".plot.tmp";
            }

            public static class Clearer
            {
                public const string OgDate = "OgDate";
                public const string OgDateThreshold = "Threshold";
                public const string OgDateThresholdArg = "07/07/2021";

                public const string OgByte = "OgByte";

                public const string ZeroByte = "ZeroByte";
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
                public const string PlotAmount = "PlotAmount";
                public const string Threads = "Threads";
                public const string Buckets = "Buckets";
                public const string Ram = "Ram";
                public const string Destination = "Destination";
                public const string PoolKey = "PoolKey";
                public const string PoolContractAddress = "PoolContractAddress";
                public const string FarmerKey = "FarmerKey";

                public const string Cache = "Cache";

                public const string MadMaxCommand = "MadMaxCommand";
                public const string ChiaCommand = "ChiaCommand";
            }

            public const string PlotterAlias = "${{" + Names.PlotterAlias + "}}";
            public const string PlotterInstance = "${{" + Names.PlotterInstance + "}}";
            public const string DriveAlias = "${{" + Names.DriveAlias + "}}";

            public const string PlotSize = "${{" + Names.PlotSize + "}}";
            public const string PlotAmount = "${{" + Names.PlotAmount + "}}";
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
