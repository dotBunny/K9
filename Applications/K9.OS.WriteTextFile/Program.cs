// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;

namespace K9.OS.WriteTextFile;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "TEXTFILE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new WriteTextFileProvider());

        try
        {
            WriteTextFileProvider provider = (WriteTextFileProvider)framework.ProgramProvider;
            if (provider.Target == null)
            {
                Log.WriteLine("The TARGET is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            if (provider.Content.Length > 0)
            {
                System.IO.File.WriteAllLines(provider.Target, provider.Content);

            }
            else
            {
                System.IO.File.WriteAllText(provider.Target, string.Empty);
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}