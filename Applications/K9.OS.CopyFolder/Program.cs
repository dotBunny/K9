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


        // string[] originalFiles = Directory.GetFiles(Input, "*", SearchOption.AllDirectories);
        // int fileCount = originalFiles.Length;
        //
        // FileUtil.EnsureFolderHierarchyExists(Output);
        //
        // for (int i = 0; i < fileCount; i++)
        // {
        //     string relativePath = Path.GetRelativePath(Input, originalFiles[i]);
        //     string outputPath = Path.GetFullPath(Path.Combine(Output, relativePath));
        //
        //     FileUtil.EnsureFileFolderHierarchyExists(outputPath);
        //
        //     File.Copy(originalFiles[i], outputPath, true);
        // }

        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}