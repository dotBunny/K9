// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.IO;
using K9.Core.Utils;

namespace K9.OS.CopyFile;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "COPYFILE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new CopyFileProvider());

        try
        {

            CopyFileProvider provider = (CopyFileProvider)framework.ProgramProvider;

            // Handle existence check
            if (provider.CheckExists)
            {
                if(provider.TargetFile && File.Exists(provider.Target))
                {
                    Log.WriteLine("File already exists at location, skipping.");
                    framework.Shutdown();
                    return;
                }
                if (Directory.Exists(provider.TargetFolder) &&
                         Directory.GetFiles(provider.TargetFolder).Length > 0)
                {
                    Log.WriteLine("Folder already exists and is not empty, skipping.");
                    framework.Shutdown();
                    return;
                }
            }

#pragma warning disable CS8604 // Possible null reference argument.
            IFileAccessor inputHandler = FileUtil.GetFileAccessor(provider.SourcePath);
#pragma warning restore CS8604 // Possible null reference argument.


            if (!provider.Extract)
            {
                using Stream inputStream = inputHandler.GetReader();
                if (provider.TargetFile)
                {
                    FileUtil.EnsureFolderHierarchyExists(Path.GetDirectoryName(provider.Target));
#pragma warning disable CS8604 // Possible null reference argument.
                    FileUtil.WriteStream(inputStream, provider.Target);
#pragma warning restore CS8604 // Possible null reference argument.
                }
                else
                {
                    FileUtil.EnsureFolderHierarchyExists(provider.TargetFolder);
#pragma warning disable CS8604 // Possible null reference argument.
                    FileUtil.WriteStream(inputStream, Path.Combine(provider.TargetFolder, Path.GetFileName(provider.SourcePath)));
#pragma warning restore CS8604 // Possible null reference argument.
                }
                inputStream.Close();
            }
            else
            {
#pragma warning disable CS8604 // Possible null reference argument.
                FileUtil.EnsureFolderHierarchyExists(provider.TargetFolder);
                CompressionUtil.Extract(provider.SourcePath, provider.TargetFolder);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }
        catch (Exception ex)
        {

            framework.ExceptionHandler(ex);
        }
    }
}