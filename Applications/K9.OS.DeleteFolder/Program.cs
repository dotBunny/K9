// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;

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


        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}