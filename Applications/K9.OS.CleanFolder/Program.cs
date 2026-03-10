// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.CleanFolder;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "CLEANFOLDER",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new CleanFolderProvider());

        try
        {
            CleanFolderProvider provider = (CleanFolderProvider)framework.ProgramProvider;

            if (provider.TargetFolder == null)
            {
                Log.WriteLine("The TARGET is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }
            
            string[] knownFiles = Directory.GetFiles(provider.TargetFolder, "*", SearchOption.AllDirectories);

            // Build out the list of files that pass the filter
            List<string> filesToDelete = new (knownFiles.Length);
            foreach(string s in knownFiles)
            {

                string? folder = Path.GetDirectoryName(s)?.Trim();
                string fileName = Path.GetFileName(s).Trim();

                if (provider.IsExcludedFileName(fileName) ||
                    provider.IsExcludedPath(folder) ||
                    provider.IsExcludedFolder(folder))
                {
                    continue;
                }
                filesToDelete.Add(s);
            }

            // Delete the files
            foreach(string s in filesToDelete)
            {
                FileUtil.ForceDeleteFile(s);
            }

            if (provider.ShouldDeleteEmptyDirectories)
            {
                string[] knownDirectories = Directory.GetDirectories(provider.TargetFolder, "*", SearchOption.AllDirectories);

                // Bottom up
                knownDirectories.Reverse();
                foreach (string folder in knownDirectories)
                {
                    string[] files = Directory.GetFiles(folder);
                    if (files.Length == 0)
                    {
                        Directory.Delete(folder, false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}