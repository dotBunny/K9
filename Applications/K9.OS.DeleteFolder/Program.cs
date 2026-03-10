// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.DeleteFolder;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "DELETEFOLDER",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new DeleteFolderProvider());

        try
        {
            DeleteFolderProvider provider = (DeleteFolderProvider)framework.ProgramProvider;
            if (provider.TargetFolder == null)
            {
                Log.WriteLine("The TARGET is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }
            
            string[] originalFiles = Directory.GetFiles(provider.TargetFolder, "*", SearchOption.AllDirectories);

            foreach(string path in originalFiles)
            {
                FileUtil.ForceDeleteFile(path);
            }
            Directory.Delete(provider.TargetFolder, true);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}