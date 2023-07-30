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
                return ProcessUtil.ExecuteProcess("tar.exe", InputFolder, $"-a -c -f {OutputPath} *", null, Line =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
            else
            {
                return ProcessUtil.ExecuteProcess("zip", InputFolder, $"-vr {OutputPath} {InputFolder} -x \"*.DS_Store\"", null, Line =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
        }
    }
}