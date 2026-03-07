// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Text.Json;


namespace K9.Core.Files
{
    [Serializable]
    public class BuildVersion
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int PatchVersion { get; set; }
        public int Changelist { get; set; }
        public int CompatibleChangelist { get; set; }
        public int IsLicenseeVersion { get; set; }
        public string BranchName { get; set; } = "UE5";

        private string? m_Path;

        public static BuildVersion? Get(string filePath)
        {
            BuildVersion? returnValue = null;
            if (File.Exists(filePath))
            {
                returnValue = JsonSerializer.Deserialize<BuildVersion>(File.ReadAllText(filePath));
                if(returnValue != null)
                {
                    returnValue.m_Path = filePath;
                }
            }
            return returnValue;
        }
    }
}