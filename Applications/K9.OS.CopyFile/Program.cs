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

            if (provider.SourcePath == null)
            {
                Log.WriteLine("The SOURCE is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            if (provider.Target == null && provider.TargetFolder == null)
            {
                Log.WriteLine("The TARGET and TARGET-FOLDER are null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

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

            IFileAccessor inputHandler = FileUtil.GetFileAccessor(provider.SourcePath);


            if (!provider.Extract)
            {
                using Stream inputStream = inputHandler.GetReader();
                if (provider.TargetFile)
                {
                    FileUtil.EnsureFolderHierarchyExists(Path.GetDirectoryName(provider.Target));
                    FileUtil.WriteStream(inputStream, provider.Target);
                }
                else if(provider.TargetFolder != null)
                {
                    FileUtil.EnsureFolderHierarchyExists(provider.TargetFolder);
                    FileUtil.WriteStream(inputStream, Path.Combine(provider.TargetFolder, Path.GetFileName(provider.SourcePath)));
                }
                inputStream.Close();
            }
            else
            {

                FileUtil.EnsureFolderHierarchyExists(provider.TargetFolder);
                CompressionUtil.Extract(provider.SourcePath, provider.TargetFolder);
            }
        }
        catch (Exception ex)
        {

            framework.ExceptionHandler(ex);
        }
    }
}