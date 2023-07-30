// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;

namespace K9.Unity.Verbs
{
    [Verb("PrepareBuild")]
    public class PrepareBuild : IVerb
    {

        [Option('i', "input", Required = true, HelpText = "Target Folder")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Destination Folder")]
        public string Output { get; set; }

        const string DebugFolder = "DebugContent";

        /// <inheritdoc />
        public bool CanExecute()
        {
            if (!Directory.Exists(Input))
            {
                Log.WriteLine("Folder not found.", "BUILD", Log.LogType.Error);
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public bool Execute()
        {
            Directory.CreateDirectory(Output);

            // Move Symbols
            string[] debugSymbols = Directory.GetFiles(Input, "*.pdb", SearchOption.AllDirectories);
            int debugSymbolCount = debugSymbols.Length;
            for(int i = 0; i < debugSymbolCount; i++)
            {
                string relativePath = Path.GetRelativePath(Input, debugSymbols[i]);
                string newPath = Path.Combine(Input, Output, relativePath);
                File.Move(debugSymbols[i], newPath, true);
            }


            // Move DoNotShip Folders
            string[] doNotShipFolders = Directory.GetDirectories(Input, "*_DoNotShip", SearchOption.AllDirectories);
            int doNotShipFoldersCount = doNotShipFolders.Length;
            for(int i = 0; i < doNotShipFoldersCount; i++)
            {
                string relativePath = Path.GetRelativePath(Input, doNotShipFolders[i]);
                string newPath = Path.Combine(Input, Output, relativePath);
                Directory.Move(doNotShipFolders[i], newPath);
            }
            return true;
        }
    }
}