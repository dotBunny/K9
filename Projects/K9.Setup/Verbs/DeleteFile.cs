﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("DeleteFile")]
    public class DeleteFile : IVerb
    {
        [Option('p', "path", Required = false, HelpText = "Path to the file to remove.")]
        public string Path { get; set; }

        public bool CanExecute()
        {
            return true;
        }

        public bool Execute()
        {
            if (File.Exists(Path))
            {
                Log.WriteLine($"Deleting {Path} ...");
                FileUtil.ForceDeleteFile(Path);
            }
            else
            {
                Log.WriteLine($"Unable to find {Path}.");
            }

            return true;
        }
    }
}