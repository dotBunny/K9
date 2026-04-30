// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Net;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.OutputToFile;

internal static class Program
{
    static void Main()
    {
       using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "OUTPUT",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new OutputToFileProvider());

        try
        {
            OutputToFileProvider provider = (OutputToFileProvider)framework.ProgramProvider;

            ProcessLogCapture outputCapture = new();

            ProcessUtil.Execute(provider.Command, provider.WorkingDirectory, provider.Arguments,
                null, outputCapture.GetAction());

            if (outputCapture.HasContent())
            {
                System.IO.File.WriteAllLines(provider.Target, outputCapture.GetLines());
                Log.WriteLine($"Content wrote to {provider.Target}.");
            }
            else
            {
                Log.WriteLine($"No content was written to {provider.Target}.", ILogOutput.LogType.Error);
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}