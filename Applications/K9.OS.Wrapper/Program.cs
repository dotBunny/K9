// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.Wrapper;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                DefaultLogCategory = "WRAPPER",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new WrapperProvider());

        try
        {
            WrapperProvider provider = (WrapperProvider)framework.ProgramProvider;

            if (provider.Command == null )
            {
                Log.WriteLine("The COMMAND is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            ProcessLogReplace replaceLog = new(ILogOutput.LogType.ExternalProcess, Path.GetFileName(provider.Command),
                provider.ReplaceWarnings, provider.ReplaceErrors)
            {
                Replacements = provider.Replacements
            };

            ProcessUtil.Execute(provider.Command, null, provider.Arguments, null,
                replaceLog.GetAction());
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}