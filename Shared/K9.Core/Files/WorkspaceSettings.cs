// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Text.Json;

namespace K9.Core.Files
{
    [Serializable]
    class WorkspaceSettings
    {
        public bool UsePerforceSyncHook = false;

        private string? m_Path;

        public static WorkspaceSettings? Get(string filePath)
        {
            WorkspaceSettings? returnValue = null;
            if (File.Exists(filePath))
            {
                returnValue = JsonSerializer.Deserialize<WorkspaceSettings>(File.ReadAllText(filePath));
                if (returnValue != null)
                {
                    returnValue.m_Path = filePath;
                }
            }
            return returnValue;
        }    
    }
}