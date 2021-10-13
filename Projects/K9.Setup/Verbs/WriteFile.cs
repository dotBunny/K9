// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("WriteFile")]
    public class WriteFile : IVerb
    {
        [Option('f', "file", Required = false, HelpText = "Path to file to write too.")]
        public string? File { get; set; }

        [Option('c', "content", Required = false, HelpText = "The content of the file to be written")]
        public string? Content { get; set; }

        [Option('l', "legacy", Required = false, HelpText = "Use legacy line writer, resulting in extra line ending.")]
        public bool AllowTrailingLine { get; set; }

        public bool CanExecute()
        {
            return !string.IsNullOrEmpty(File);
        }

        public bool Execute()
        {
            string folder = Path.GetDirectoryName(File) ?? string.Empty;
            if (folder != string.Empty && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (string.IsNullOrEmpty(File))
            {
                return false;
            }

            // Some single line fixes
            if (!string.IsNullOrEmpty(Content))
            {
                Content = Content.Replace("___SPACE___", " ");
                if (AllowTrailingLine)
                {
                    System.IO.File.WriteAllLines(File, Content.Split("___NEWLINE___"));
                }
                else
                {
                    FileUtil.WriteAllLinesNoExtraLine(File, Content.Split("___NEWLINE___"));
                }

            }
            else
            {
                System.IO.File.WriteAllText(File, string.Empty);
            }

            return true;
        }
    }
}