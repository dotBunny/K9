// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text.RegularExpressions;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("ReplaceInFile")]
    public class ReplaceInFile
    {
        [Option('i', "input", Required = true, HelpText = "The target file path")]
        public string Input { get; set; }

        [Option('r', "replace", Required = true, HelpText = "Regex based search")]
        public string ReplaceRegex { get; set; }

        [Option('w', "with", Required = true, HelpText = "The folder root to copy to.")]
        public string Content { get; set; }

        public bool CanExecute()
        {
            if (File.Exists(Input))
            {
                return true;
            }
            return false;
        }

        public bool Execute()
        {
            string fileContent = File.ReadAllText(Input);

            Regex regex = new Regex(ReplaceRegex, RegexOptions.Compiled);

            string updatedContent = regex.Replace(fileContent, Content);

            File.WriteAllText(Input, updatedContent);

            return fileContent != updatedContent;
        }
    }
}