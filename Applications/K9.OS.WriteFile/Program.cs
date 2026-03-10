// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;

namespace K9.OS.WriteFile;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "WRITEFILE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new WriteFileProvider());

        try
        {
            WriteFileProvider provider = (WriteFileProvider)framework.ProgramProvider;


        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}