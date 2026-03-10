// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.ExtractArchive;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "XARCHIVE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new ExtractArchiveProvider());

        try
        {
            ExtractArchiveProvider provider = (ExtractArchiveProvider)framework.ProgramProvider;

            // Handle existence check
            if (provider.CheckExists &&
                Directory.Exists(provider.TargetFolder) &&
                Directory.GetFiles(provider.TargetFolder).Length > 0)
            {
                Log.WriteLine("Folder already exists and is not empty, skipping.");
                framework.Shutdown();
                return;
            }

            if (provider.Source == null)
            {
                Log.WriteLine("The SOURCE is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            string? targetFolder = string.IsNullOrEmpty(provider.TargetFolder) ? Path.GetDirectoryName(provider.Source) : provider.TargetFolder;

            if(targetFolder == null)
            {
                Log.WriteLine("The TARGET-FOLDER is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            CompressionUtil.Extract(provider.Source, targetFolder);


        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}