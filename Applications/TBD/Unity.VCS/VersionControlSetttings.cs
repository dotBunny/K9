// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using K9.Utils;

namespace K9.Unity.Verbs
{
    [Verb("VersionControlSettings")]
    public class VersionControlSettings : IVerb
    {
        [Option('c', "clear", Required = false, Default = true)]
        public bool ClearSettings { get; set; } = true;

        [Option('f', "folder", Required = true, HelpText = "Target Folder")]
        public string Folder { get; set; }

        public bool CanExecute()
        {
            return Directory.Exists(GetFolder());
        }

        public bool Execute()
        {
            string settingsPath = Path.Combine(Path.GetFullPath(GetFolder()), "ProjectSettings",
                "VersionControlSettings.asset");
            if (!File.Exists(settingsPath))
            {
                Log.WriteLine($"Unable to find VersionControlSettings.asset at {settingsPath}");
                return false;
            }

            string[] settingsContent = File.ReadAllLines(settingsPath);

            if (settingsContent.Length == 0)
            {
                Log.WriteLine("No content found in VersionControlSettings.asset");
                return false;
            }

            int count = settingsContent.Length;
            for (int i = 0; i < count; i++)
            {
                if (settingsContent[i].Trim().StartsWith("m_Mode:"))
                {
                    string original = settingsContent[i].Trim().Replace("m_Mode:", "").Trim();
                    Log.WriteLine($"VCS original value: {original}");
                    if (ClearSettings)
                    {
                        settingsContent[i] = settingsContent[i].Replace(original, "Visible Meta Files");
                        Log.WriteLine("VCS set to \"Visible Meta Files\"");
                    }

                    break;
                }
            }


            settingsPath.MakeWritable();
            File.WriteAllLines(settingsPath, settingsContent);

            return true;
        }

        private string GetFolder()
        {
            return string.IsNullOrEmpty(Folder) ? Core.WorkspaceRoot : Folder;
        }
    }
}