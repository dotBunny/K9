// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;

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


        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}