// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;
using System.Reflection;
using K9.Core.Extensions;

namespace K9.Core.Utils;

public static class WorkspaceUtil
{
    // UE SPECIFIC
    static readonly int k_CachedGenerateProjectFilesHash = "GenerateProjectFiles.bat".GetStableUpperCaseHashCode();
    static readonly int k_CachedSetupHash = "Setup.bat".GetStableUpperCaseHashCode();
    static string? s_CachedWorkspaceRoot;

    public static string? GetWorkspaceRoot(string? workingDirectory = null)
    {
        // Use our cached version!
        if (s_CachedWorkspaceRoot != null)
        {
            return s_CachedWorkspaceRoot;
        }

        // If we don't have anything provided, we need to start somewhere.
        if (workingDirectory == null)
        {
            Assembly? workingAssembly = Assembly.GetEntryAssembly();
            if (workingAssembly != null)
            {
                DirectoryInfo? parentInfo = Directory.GetParent(workingAssembly.Location);
                if (parentInfo != null)
                {
                    workingDirectory = parentInfo.FullName;
                }
            }
            if (workingDirectory == null)
            {
                Log.WriteLine("Unable to determine assembly entry point.", ILogOutput.LogType.Error);
                return null;
            }
        }


        // Check local files for marker
        string[] localFiles = Directory.GetFiles(workingDirectory);
        int localFileCount = localFiles.Length;
        int foundCount = 0;

        // Iterate over the directory files
        for (int i = 0; i < localFileCount; i++)
        {
            int fileNameHash = Path.GetFileName(localFiles[i]).GetStableUpperCaseHashCode();

            if (fileNameHash == k_CachedGenerateProjectFilesHash)
            {
                foundCount++;
            }

            if (fileNameHash == k_CachedSetupHash)
            {
                foundCount++;
            }
        }

        // We know this is the root based on found files
        if (foundCount == 2)
        {
            s_CachedWorkspaceRoot = workingDirectory;
            return s_CachedWorkspaceRoot;
        }

        // Go back up another directory
        DirectoryInfo? parent = Directory.GetParent(workingDirectory);
        return parent != null ? GetWorkspaceRoot(parent.FullName) : null;
    }
}