// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using K9.Utils;

namespace K9.TeamCity.Verbs
{
    [Verb("ExtractArtifacts")]
    public class ExtractArtifacts : IVerb
    {
        [Option('s', "split", Required = false, HelpText = "Split file name based on.", Default = "_")]
        public string SplitChar { get; set; }

        [Option('a', "folder", Required = false, HelpText = "The index of the split name to use as the root folder", Default = 0)]
        public int FolderIndex { get; set; }

        [Option('b', "subfolder", Required = false, HelpText = "The index of the split name to use as the sub folder", Default = 3)]
        public int SubFolderIndex { get; set; }

        [Option('i', "input", Required = true, HelpText = "Input folder to search for zips")]
        public string InputFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output folder to serve as the root of extracted artifacts.")]

        public string OutputFolder { get; set; }

        public bool CanExecute()
        {
            return Directory.Exists(InputFolder);
        }

        public bool Execute()
        {
            string[] files = Directory.GetFiles(InputFolder, "*.zip", SearchOption.AllDirectories);
            int fileCount = files.Length;
            if (fileCount == 0) return false;

            if (!Directory.Exists(OutputFolder))
            {
                FileUtil.EnsureFolderHierarchyExists(OutputFolder);FileUtil.EnsureFolderHierarchyExists(OutputFolder);
            }

            int returnValue = 0;

            for (int i = 0; i < fileCount; i++)
            {
                // Build naming
                string currentFile = files[i];
                string wholeFilename = Path.GetFileNameWithoutExtension(currentFile);
                string[] splitFilename = wholeFilename.Split(SplitChar, System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
                string folderName = splitFilename[FolderIndex];
                string subFolderName = splitFilename[SubFolderIndex];

                string targetFolder = Path.GetFullPath(Path.Combine(OutputFolder, folderName, subFolderName));
                FileUtil.EnsureFolderHierarchyExists(targetFolder);

                Log.WriteLine($"Uncompressing {currentFile} to {targetFolder} ...", "TEAMCITY");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    returnValue = ProcessUtil.ExecuteProcess("tar.exe", targetFolder, $"-xf {currentFile} -C {targetFolder}", null, (ProcessID, Line) =>
                    {
                        System.Console.WriteLine(Line);
                    });
                }
                else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    returnValue = ProcessUtil.ExecuteProcess("ditto", targetFolder, $"-x -k {currentFile} {targetFolder}", null, (ProcessID, Line) =>
                    {
                        System.Console.WriteLine(Line);
                    });
                }
                else
                {
                    returnValue = ProcessUtil.ExecuteProcess("tar", targetFolder, $"-xf {currentFile} -C {targetFolder}", null, (ProcessID, Line) =>
                    {
                        System.Console.WriteLine(Line);
                    });
                }
            }
            return returnValue == 0;
        }
    }
}
