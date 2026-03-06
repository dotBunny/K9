// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("SetEnvironmentVariable")]
    public class SetEnvironmentVariable : IVerb
    {
        [Option('f', "file", Required = false, HelpText = "Path to file to parse in var=val format.")]
        public string File { get; set; }

        [Option('n', "name", Required = false, HelpText = "The variable name")]
        public string Name { get; set; }

        [Option('v', "value", Required = false, HelpText = "The variable value")]
        public string Value { get; set; }

        public bool CanExecute()
        {
            // Have file, but doesnt exist
            if (!string.IsNullOrEmpty(File) && !System.IO.File.Exists(File))
            {
                return false;
            }

            // A value, but no name
            if (!string.IsNullOrEmpty(Value) && string.IsNullOrEmpty(Name))
            {
                return false;
            }

            return true;
        }

        public bool Execute()
        {
            // Process File
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

            return true;
        }
    }
}