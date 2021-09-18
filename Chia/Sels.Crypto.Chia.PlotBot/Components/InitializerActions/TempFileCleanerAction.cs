using Sels.Core.Components.Logging;
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
    public class TempFileCleanerAction : IPlotBotInitializerAction
    {
        public void Handle(Plotter[] plotters, Drive[] drives)
        {
            using (LoggingServices.TraceMethod(this))
            {
                if (plotters.HasValue() && drives.HasValue())
                {
                    LoggingServices.Log($"Checking for plots that failed to copy for {plotters.Length} plotters and {drives.Length} drives");

                    foreach (var plotter in plotters)
                    {
                        var copyExtension = plotter.PlotProgressParser.TransferExtension;

                        if (copyExtension.HasValue())
                        {
                            LoggingServices.Debug($"Searching for files with extension <{copyExtension}>");

                            foreach (var drive in drives)
                            {
                                LoggingServices.Debug($"Checking drive {drive.Alias} with the settings from plotter {plotter.Alias}");

                                var files = drive.Directory.Source.GetFiles($"*{copyExtension}", SearchOption.AllDirectories).ToArray();


                                if (files.HasValue())
                                {
                                    int deletedFileCount = 0;

                                    foreach (var file in files)
                                    {
                                        if ((DateTime.Now - file.LastWriteTime).TotalMinutes > TimeSpan.FromMinutes(1).TotalMinutes)
                                        {
                                            LoggingServices.Trace($"Deleting incomplete plot file <{file.FullName}>");
                                            file.Delete();
                                            deletedFileCount++;
                                        }
                                        else
                                        {
                                            LoggingServices.Trace($"File <{file.FullName}> was modified less than 1 minute ago so not deleting");
                                        }
                                    }

                                    LoggingServices.Log($"Deleted {deletedFileCount} failed plot files from the total {files.Length} files found");
                                }
                                else
                                {
                                    LoggingServices.Log($"No failed plots found in drive {drive.Alias} with the settings from plotter {plotter.Alias}");
                                }
                            }
                        }
                        else
                        {
                            LoggingServices.Warning($"Could not get copy extension from plotter {plotter.Alias}");
                        }
                    }
                }
            }
        }
    }
}
