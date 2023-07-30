// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using K9.IO;

using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("CopyFolder")]
    public class CopyFolder
    {
        [Option('i', "input", Required = true, HelpText = "The folder to start copying from.")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "The folder root to copy to.")]
        public string Output { get; set; }

        public bool CanExecute()
        {
            if (Directory.Exists(Input))
            {
                return true;
            }
            return false;
        }

        public bool Execute()
        {

            string[] originalFiles = Directory.GetFiles(Input, "*", SearchOption.AllDirectories);
            int fileCount = originalFiles.Length;

            FileUtil.EnsureFolderHierarchyExists(Output);

            for (int i = 0; i < fileCount; i++)
            {
                string relativePath = Path.GetRelativePath(Input, originalFiles[i]);
                string outputPath = Path.GetFullPath(Path.Combine(Output, relativePath));

                FileUtil.EnsureFileFolderHierarchyExists(outputPath);

                File.Copy(originalFiles[i], outputPath, true);
            }

            return true;
        }
    }
}