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

            // Handle User-level file
            if (provider.UserTarget != null)
            {
                string[] lines = System.IO.File.ReadAllLines(provider.UserTarget);
                foreach (string line in lines)
                {
                    string[] split = line.Split('=', 2);
                    if (split.Length != 2)
                    {
                        continue;
                    }

                    Log.WriteLine($"SET User Environment Variable {split[0]}={split[1]}");
                    Environment.SetEnvironmentVariable(split[0], split[1], EnvironmentVariableTarget.User);
                }
            }

            // Handle User-level arguments
            if (provider is { UserKeys: not null, UserValues: not null })
            {
                int count = provider.UserKeys.Length;
                for(int i = 0; i < count; i++)
                {
                    Log.WriteLine($"SET User Environment Variable {provider.UserKeys[i]}={provider.UserValues[i]}");
                    Environment.SetEnvironmentVariable(provider.UserKeys[i], provider.UserValues[i], EnvironmentVariableTarget.User);
                }
            }

            // TODO: Should this be wrapped in an elevation check?

            // Handle System-wide file
            if (provider.SystemTarget != null)
            {
                string[] lines = System.IO.File.ReadAllLines(provider.SystemTarget);
                foreach (string line in lines)
                {
                    string[] split = line.Split('=', 2);
                    if (split.Length != 2)
                    {
                        continue;
                    }

                    Log.WriteLine($"SET Machine Environment Variable {split[0]}={split[1]}");
                    Environment.SetEnvironmentVariable(split[0], split[1], EnvironmentVariableTarget.Machine);
                }
            }

            // Handle System-wide arguments
            if (provider is { SystemKeys: not null, SystemValues: not null })
            {
                int count = provider.SystemKeys.Length;
                for(int i = 0; i < count; i++)
                {
                    Log.WriteLine($"SET User Environment Variable {provider.SystemKeys[i]}={provider.SystemValues[i]}");
                    Environment.SetEnvironmentVariable(provider.SystemKeys[i], provider.SystemValues[i], EnvironmentVariableTarget.Machine);
                }
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}