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
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
            if (provider.Content.Length > 0)
            {
                System.IO.File.WriteAllLines(provider.Target, provider.Content);

            }
            else
            {
                System.IO.File.WriteAllText(provider.Target, string.Empty);
            }
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}