// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.CopyFolder;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "COPYFOLDER",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new CopyFolderProvider());

        try
        {
            CopyFolderProvider provider = (CopyFolderProvider)framework.ProgramProvider;


#pragma warning disable CS8604 // Possible null reference argument.
            string[] originalFiles = Directory.GetFiles(provider.SourceFolder, "*", SearchOption.AllDirectories);
#pragma warning restore CS8604 // Possible null reference argument.
            int fileCount = originalFiles.Length;

            if (provider.ClearTargetFolder && Directory.Exists(provider.TargetFolder))
            {
                Directory.Delete(provider.TargetFolder, true);
            }
            FileUtil.EnsureFolderHierarchyExists(provider.TargetFolder);

            for (int i = 0; i < fileCount; i++)
            {
                string relativePath = Path.GetRelativePath(provider.SourceFolder, originalFiles[i]);
#pragma warning disable CS8604 // Possible null reference argument.
                string outputPath = Path.GetFullPath(Path.Combine(provider.TargetFolder, relativePath));
#pragma warning restore CS8604 // Possible null reference argument.

                FileUtil.EnsureFileFolderHierarchyExists(outputPath);
                File.Copy(originalFiles[i], outputPath, true);
            }

        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}