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
#pragma warning disable CS8604 // Possible null reference argument.
            string[] knownFiles = Directory.GetFiles(provider.TargetFolder, "*", SearchOption.AllDirectories);
#pragma warning restore CS8604 // Possible null reference argument.

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