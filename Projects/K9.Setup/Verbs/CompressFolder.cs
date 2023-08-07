// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("CompressFolder")]
    public class CompressFolder : IVerb
    {
        [Option('i', "input", Required = true, HelpText = "Input root.")]
        public string InputFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file path.")]
        public string OutputPath { get; set; }

        public bool CanExecute()
        {
            return Directory.Exists(InputFolder);
        }

        public bool Execute()
        {
            FileUtil.EnsureFileFolderHierarchyExists(OutputPath);

            // Figure out built-in zip compressor
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows OK using tar to generate zip files without issue
                return ProcessUtil.ExecuteProcess("tar.exe", InputFolder, $"-acf {OutputPath} *", null, (ProcessID, Line) =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Mac we'll use the specific mac command to ensure the folders maintain flags
                return ProcessUtil.ExecuteProcess("ditto", InputFolder, $"-c -k --sequesterRsrc {InputFolder} {OutputPath}", null, (ProcessID, Line) =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
            else
            {
                return ProcessUtil.ExecuteProcess("tar", InputFolder, $"-acf {OutputPath} *", null, (ProcessID, Line) =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
        }
    }
}