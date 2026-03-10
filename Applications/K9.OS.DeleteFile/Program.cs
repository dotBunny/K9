// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.DeleteFile;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "DELETEFILE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new DeleteFileProvider());

        try
        {
            DeleteFileProvider provider = (DeleteFileProvider)framework.ProgramProvider;
            FileUtil.ForceDeleteFile(provider.TargetFile);

        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}