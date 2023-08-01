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
    [Verb("ExtractToFolder")]
    public class ExtractToFolder : IVerb
    {
        [Option('i', "input", Required = true, HelpText = "Input file")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output folder path.")]
        public string OutputFolder { get; set; }

        public bool CanExecute()
        {
            return File.Exists(InputFile);
        }

        public bool Execute()
        {
            // Figure out built-in zip compressor
            if (!Directory.Exists(OutputFolder))
            {
                FileUtil.EnsureFolderHierarchyExists(OutputFolder);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ProcessUtil.ExecuteProcess("tar.exe", OutputFolder, $"-xf {InputFile} -C {OutputFolder}", null, Line =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return ProcessUtil.ExecuteProcess("ditto", OutputFolder, $"-xk  {InputFile} {OutputFolder}", null, Line =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
            else
            {
                return ProcessUtil.ExecuteProcess("tar", OutputFolder, $"-xf  {InputFile} -C {OutputFolder}", null, Line =>
                {
                    Console.WriteLine(Line);
                }) == 0;
            }
        }
    }
}