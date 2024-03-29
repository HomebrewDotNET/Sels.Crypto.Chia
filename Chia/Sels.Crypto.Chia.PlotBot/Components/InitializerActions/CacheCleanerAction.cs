﻿using Sels.Core.Components.Logging;
using Sels.Core.Extensions;
using Sels.Crypto.Chia.PlotBot.Contracts;
using Sels.Crypto.Chia.PlotBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sels.Crypto.Chia.PlotBot.Components.InitializerActions
{
    /// <summary>
    /// Deletes all files in the cache directories of the plotters.
    /// </summary>
    public class CacheCleanerAction : IPlotBotInitializerAction
    {
        public void Handle(Plotter[] plotters, Drive[] drives)
        {
            using (LoggingServices.TraceMethod(this))
            {
                if (plotters.HasValue())
                {
                    LoggingServices.Log($"Cleaning up caches for {plotters.Length} plotters");

                    foreach(var plotter in plotters)
                    {
                        LoggingServices.Debug($"Cleaning caches for {plotter.Alias}");

                        foreach(var cache in plotter.Caches)
                        {
                            LoggingServices.Debug($"Searching for files in cache <{cache.Directory.Source.FullName}>");

                            var files = cache.Directory.Source.GetFiles("*", SearchOption.AllDirectories).ToArray();

                            foreach (var file in files)
                            {
                                HandleFile(plotter, cache, file);
                            }

                            LoggingServices.Log($"Deleted {files.Length} files in cache <{cache.Directory.Source.FullName}>");
                        }                        
                    }
                }
            }
        }

        protected virtual void HandleFile(Plotter plotter, PlotterCache cache, FileInfo file)
        {
            LoggingServices.Trace($"Deleting file <{file.FullName}>");
            file.Delete();
        }
    }

    public class TestCacheCleanerAction : CacheCleanerAction
    {
        protected override void HandleFile(Plotter plotter, PlotterCache cache, FileInfo file)
        {
            LoggingServices.Log($"Plot Bot would have deleted file <{file}> from cache <{cache.Directory.FullName}> for Plotter <{plotter.Alias}>");
        }
    }
}
