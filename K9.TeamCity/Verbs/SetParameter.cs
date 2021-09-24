using System;
using CommandLine;

namespace K9.TeamCity.Verbs
{
    [Verb("SetParameter")]
    public class SetParameter : IVerb
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
                        Console.WriteLine($"##teamcity[setParameter name='{split[0]}' value='{split[1]}']");
                    }
                }
            }

            if (!string.IsNullOrEmpty(Name))
            {
                Console.WriteLine($"##teamcity[setParameter name='{Name}' value='{Value}']");
            }

            return true;
        }
    }
}