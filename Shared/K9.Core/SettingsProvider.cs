// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;

namespace K9.Core;

/// <remarks>
///     If these are changed, you should triple check for anything in the Bootstrap project that would need updating.
///     Because it does not reference anything else, changing here won't change Bootstrap.
/// </remarks>
public class SettingsProvider
{
    public const string PerforcePort = "ssl:perforce.dotbunny.com:1666";
    public const string PerforceCharacterSet = "none";
    public const string PerforceIgnoreFileName = "p4ignore.txt";
    public const string PerforceConfigFileName = "p4config.txt";
    public const string PerforceCustomToolsFileName = "p4v-custom-tools.xml";
    public readonly string PerforceConfigFile;

    public const string BuildHashFileName = "K9_BUILD_SHA";

    public readonly string RootFolder;
    public readonly string LogsFolder;

    public readonly string TempFile;

    public readonly string K9Folder;

    public readonly string SourceFolder;
    public readonly string BoostrapLibrary;
    public readonly string BinariesFolder;
    public readonly string DefaultsFolder;
    public readonly string WorkspaceSettingsFile;
    public readonly string WorkspaceVersionFile;

    public readonly string UnrealProjectsFolder;
    public readonly string UnrealEngineBuildBatchFilesFolder;
    public readonly string UnrealSourceSolutionFile;
    public readonly string UnrealEngineBuildVersionFile;

    readonly string m_AppDataLocalFolder;
    readonly string m_AppDataLocalLowFolder;
    readonly string m_AppDataRoamingFolder;

    public SettingsProvider(string root)
    {
        RootFolder = root;

        K9Folder = Path.Combine(RootFolder, "K9");
        BoostrapLibrary = Path.Combine(K9Folder, "Bootstrap", "Bootstrap.dll");
        LogsFolder = Path.Combine(RootFolder, "Logs");
        TempFile = Path.Combine(RootFolder, "k9.tmp");


        SourceFolder = Path.Combine(K9Folder, "Source");
        BinariesFolder = Path.Combine(K9Folder, "Binaries");
        DefaultsFolder = Path.Combine(K9Folder, "Defaults");

        WorkspaceSettingsFile = Path.Combine(K9Folder, "K9_SETTINGS");
        WorkspaceVersionFile = Path.Combine(K9Folder, "K9_WORKSPACE");

        UnrealSourceSolutionFile = Path.Combine(RootFolder, "UE5.sln");
        UnrealEngineBuildBatchFilesFolder = Path.Combine(RootFolder, "Engine", "Build", "BatchFiles");
        UnrealEngineBuildVersionFile = Path.Combine(RootFolder, "Engine", "Build", "Build.version");
        UnrealProjectsFolder = Path.Combine(RootFolder, "Projects");

        PerforceConfigFile = Path.Combine(RootFolder, PerforceConfigFileName);

        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..");
        m_AppDataLocalFolder = Path.Combine(appDataFolder, "Local");
        m_AppDataLocalLowFolder = Path.Combine(appDataFolder, "LocalLow");
        m_AppDataRoamingFolder = Path.Combine(appDataFolder, "Roaming");
    }

    public string ReplaceKeywords(string sourceString)
    {
        return sourceString.Replace("{ROOT}", RootFolder)
            // ReSharper disable once StringLiteralTypo
            .Replace("{LOCALLOW}", m_AppDataLocalLowFolder)
            .Replace("{LOCAL}", m_AppDataLocalFolder)
            .Replace("{ROAMING}", m_AppDataRoamingFolder);
    }

    public void Output()
    {
        Log.WriteLine("Settings:");
        Log.WriteLine($"Root: {RootFolder}", "LOCATION", ILogOutput.LogType.Info);
    }
}