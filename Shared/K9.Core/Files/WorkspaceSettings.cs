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
        private string? m_Path;

        public static WorkspaceSettings? Get(string filePath)
        {
            WorkspaceSettings? returnValue = null;
            if (!File.Exists(filePath))
            {
                return returnValue;
            }

            returnValue = JsonSerializer.Deserialize<WorkspaceSettings>(File.ReadAllText(filePath));
            returnValue?.m_Path = filePath;
            return returnValue;
        }
    }
}