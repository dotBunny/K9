// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using CommandLine;
using K9.Utils;

namespace K9.TeamCity.Verbs
{
    [Verb("CleanFolder")]
    public class CleanFolder : IVerb
    {
        [Option('s', "skip", Required = false, HelpText = "Comma-delimitted safe query list.")]
        public string SafeQuery { get; set; }

        [Option('i', "input", Required = true, HelpText = "Input folder to clean")]
        public string InputFolder { get; set; }

        public bool CanExecute()
        {
            return true;
        }

        public bool Execute()
        {
            if (!Directory.Exists(InputFolder)) return true;

            string[] directories = Directory.GetDirectories(InputFolder, "*", SearchOption.TopDirectoryOnly);
            int directoryCount = directories.Length;
            Log.WriteLine($"Found {directoryCount} Directories", "TEAMCITY");
            List<string> directoriesToDelete = new List<string>(directoryCount);

            string[] files = Directory.GetFiles(InputFolder, "*", SearchOption.TopDirectoryOnly);
            int filesCount = files.Length;
            Log.WriteLine($"Found {filesCount} Files", "TEAMCITY");

            List<string> filesToDelete = new List<string>(filesCount);

            SafeQuery ??= string.Empty;
            string[] filters = SafeQuery.Split(",", System.StringSplitOptions.RemoveEmptyEntries);
            int filtersCount = filters.Length;
            Log.WriteLine($"Filtering based on {filtersCount} filters ...", "TEAMCITY");

            // Handle Directories
            for(int i = 0; i < directoryCount; i++)
            {
                string relativeName = Path.GetRelativePath(InputFolder, directories[i]);
                bool foundQuery = false;
                for(int j = 0; j < filtersCount; j++)
                {
                    if (relativeName.Contains(filters[j]))
                    {
                        foundQuery = true;
                        break;
                    }
                }
                if (foundQuery) continue;
                directoriesToDelete.Add(directories[i]);
            }

            // Handle Files
            for (int i = 0; i < filesCount; i++)
            {
                string relativeName = Path.GetRelativePath(InputFolder, files[i]);
                bool foundQuery = false;
                for (int j = 0; j < filtersCount; j++)
                {
                    if (relativeName.Contains(filters[j]))
                    {
                        foundQuery = true;
                        break;
                    }
                }
                if (foundQuery) continue;
                filesToDelete.Add(files[i]);
            }

            int directoriesToDeleteCount = directoriesToDelete.Count;
            int filesToDeleteCount = filesToDelete.Count;


            Log.WriteLine($"Deleting {filesToDeleteCount} Fils ...", "TEAMCITY");
            for(int i = 0; i < filesToDeleteCount; i++)
            {
                string path = filesToDelete[i];
                Log.WriteLine($"Removing File: {path} ...", "TEAMCITY");
                FileUtil.ForceDeleteFile(path);
            }

            Log.WriteLine($"Deleting {directoriesToDeleteCount} Directories ...", "TEAMCITY");

            for (int i = 0; i < directoriesToDeleteCount; i++)
            {
                string path = directoriesToDelete[i];
                Log.WriteLine($"Removing Directory: {path} ...", "TEAMCITY");
                Directory.Delete(path, true);
            }

            return true;
        }
    }
}
