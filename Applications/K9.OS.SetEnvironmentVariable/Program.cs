// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;

namespace K9.OS.SetEnvironmentVariable;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "SETENV",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new SetEnvironmentVariableProvider());

        try
        {
            SetEnvironmentVariableProvider provider = (SetEnvironmentVariableProvider)framework.ProgramProvider;


        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}