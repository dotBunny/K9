// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.CreateArchive;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "CARCHIVE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new CreateArchiveProvider());

        try
        {
            CreateArchiveProvider provider = (CreateArchiveProvider)framework.ProgramProvider;
            if (provider.SourceFolder == null ||
                provider.Target == null)
            {
                Log.WriteLine("One or more of the required parameters are null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            CompressionUtil.Create(provider.SourceFolder, provider.Source, provider.Target);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}