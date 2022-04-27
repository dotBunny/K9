// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using K9.IO;
using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("Zip")]
    public class Zip : IVerb
    {
        [Option('x', "extract", Required = false, HelpText = "Extract destination.")]
        public string ExtractFolder { get; set; }

        [Option('c', "compress", Required = false, HelpText = "The content of the file to be written")]
        public string CompressFilter { get; set; }

        [Option('t', "target", Required = true, HelpText = "A target zip needs to be set..")]
        public string Target { get; set; }

        public bool CanExecute()
        {
            return !string.IsNullOrEmpty(Target) && (!string.IsNullOrEmpty(CompressFilter) || !string.IsNullOrEmpty(ExtractFolder));
        }

        public bool Execute()
        {
            if (!string.IsNullOrEmpty(CompressFilter))
            {
                List<string> filesToAdd = new List<string>();
                string[] filters = CompressFilter.Split(',');
                int filterCount = filters.Length;
                for (int i = 0; i < filterCount; i++)
                {
                    string filter = filters[i];
                    string fileName = Path.GetFileName(filter);
                    if (fileName.Contains("*"))
                    {
                        string wildCardDirectory = Path.GetDirectoryName(filter);
                        if (wildCardDirectory == null)
                        {
                            continue;
                        }
                        string[] foundFiles = Directory.GetFiles(wildCardDirectory, fileName,
                            SearchOption.TopDirectoryOnly);
                        filesToAdd.AddRange(foundFiles);
                    }
                    else if(File.Exists(filter))
                    {
                        filesToAdd.Add(filter);
                    }
                }

                if (filesToAdd.Count > 0)
                {
                    return Compression.AddToZip(Target, filesToAdd.ToArray());
                }

                Log.WriteLine("No files to add to zip", Program.Instance.DefaultLogCategory, Log.LogType.Notice);
                return true;
            }
            throw new NotImplementedException();
        }
    }
}