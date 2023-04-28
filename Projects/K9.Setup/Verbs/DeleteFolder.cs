// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("DeleteFolder")]
    public class DeleteFolder : IVerb
    {
        [Option('f', "folder", Required = false, HelpText = "Path to the folder to remove.")]
        public string Folder { get; set; }

        public bool CanExecute()
        {
            return true;
        }

        public bool Execute()
        {
            if (Directory.Exists(Folder))
            {
                Log.WriteLine($"Deleting {Folder} ...");
                Directory.Delete(Folder, true);
            }
            else
            {
                Log.WriteLine($"Folder {Folder} was not found.");
            }

            return true;
        }
    }
}