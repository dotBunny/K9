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



            /*  // Process File
            if (!string.IsNullOrEmpty(File))
            {
                string[] lines = System.IO.File.ReadAllLines(File);
                foreach (string line in lines)
                {
                    string[] split = line.Split('=', 2);
                    if (split.Length == 2)
                    {
                        Log.WriteLine($"SET User Environment Variable {split[0]}={split[1]} (",
                            Program.Instance.DefaultLogCategory);
                        Environment.SetEnvironmentVariable(split[0], split[1], EnvironmentVariableTarget.User);
                    }
                }
            }

            if (!string.IsNullOrEmpty(Name))
            {
                Log.WriteLine($"SET User Environment Variable {Name}={Value}", Program.Instance.DefaultLogCategory);
                Environment.SetEnvironmentVariable(Name, Value, EnvironmentVariableTarget.User);
            }

            return true;*/
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}