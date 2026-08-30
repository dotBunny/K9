// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using K9.Core;
using K9.Core.Utils;

namespace K9.Service.PerforceBackup;

public class PerforceBackupConfig
{
    public const string DefaultFileName = "backup.json";

    // Required for JSON
    // ReSharper disable PropertyCanBeMadeInitOnly.Global
    public string DataRoot { get; set; } = @"D:\PerforceBackup\";
    public int SyncSleep { get; set; } = 5 * 60;
    public string? LogFolder { get; set; }

    public string? PerforcePort { get; set; } = "ssl:perforce.dotbunny.com:1666";
    public string? PerforceUsername { get; set; }
    public string? PerforcePassword { get; set; }
    public string PerforceClientName { get; set; } = "K9_PerforceBackup_Sync";
    public string PerforceWorkspaceStreamName { get; set; } = "//UE5/DETHOL-Main";

    public string BackupTimeOfDay { get; set; } = "03:00";
    public string? BackupSourceFolder { get; set; }
    public string BackupSourceRelativeRoot { get; set; } = string.Empty;
    public string BackupName { get; set; } = "Backup";
    public string BackupNameTemplate { get; set; } = "$Name_$Date";
    public string StagingRelativeRoot { get; set; } = "staging";
    public bool VerifyArchive { get; set; } = true;
    public bool RetryOnFailure { get; set; } = true;
    public int MinimumFreeSpaceGigabytes { get; set; }

    public string? SevenZipPath { get; set; }
    public string? SevenZipArguments { get; set; }
    public string NetworkShare { get; set; } = @"\\192.168.20.21\PerforceBackup";
    public string? NetworkMapping { get; set; }
    public string? NetworkUsername { get; set; }
    public string? NetworkPassword { get; set; }
    public string? NetworkCredentialsFile { get; set; }
    public string NetworkTargetFolder { get; set; } = string.Empty;
    public int KeepCount { get; set; } = 7;

    // ReSharper restore PropertyCanBeMadeInitOnly.Global

    public static PerforceBackupConfig Get(string? jsonPath = null)
    {
        JsonSerializerOptions jsonSettings = new()
        {
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        try
        {
            PerforceBackupConfig? foundConfig;
            if (jsonPath == null)
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);
                if (!File.Exists(filePath)) return new PerforceBackupConfig();

                string content = File.ReadAllText(filePath);

                foundConfig = JsonSerializer.Deserialize<PerforceBackupConfig>(content, jsonSettings)!;
            }
            else
            {
                if (!File.Exists(jsonPath)) return Get();

                string content = File.ReadAllText(jsonPath);

                foundConfig = JsonSerializer.Deserialize<PerforceBackupConfig>(content, jsonSettings);
                if (foundConfig == null) return Get();
            }

            CleanUpConfig(foundConfig);
            return foundConfig;
        }
        catch (Exception e)
        {
            Log.WriteLine(e.Message, ILogOutput.LogType.Error);
            if (e.StackTrace != null)
            {
                Log.WriteLine(e.StackTrace, ILogOutput.LogType.Error);
            }
            return Get();
        }
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(DataRoot))
        {
            Log.WriteLine("A DataRoot is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforcePort))
        {
            Log.WriteLine("A PerforcePort is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceUsername))
        {
            Log.WriteLine("A PerforceUsername is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforcePassword))
        {
            Log.WriteLine("A PerforcePassword is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceClientName))
        {
            Log.WriteLine("A PerforceClientName is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceWorkspaceStreamName))
        {
            Log.WriteLine("A PerforceWorkspaceStreamName is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (!TryGetBackupTimeOfDay(out TimeSpan _))
        {
            Log.WriteLine($"The BackupTimeOfDay ({BackupTimeOfDay}) could not be parsed, use a 24-hour HH:mm value.",
                ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(BackupSourceFolder) && string.IsNullOrEmpty(BackupSourceRelativeRoot))
        {
            Log.WriteLine("A BackupSourceFolder or BackupSourceRelativeRoot is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(BackupNameTemplate))
        {
            Log.WriteLine("A BackupNameTemplate is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(NetworkShare))
        {
            Log.WriteLine("A NetworkShare is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }

        return true;
    }

    public static void CleanUpConfig(PerforceBackupConfig config)
    {
        if (config.SyncSleep <= 0)
        {
            config.SyncSleep = 5 * 60;
        }

        if (config.KeepCount < 0)
        {
            config.KeepCount = 0;
        }

        if (config.MinimumFreeSpaceGigabytes < 0)
        {
            config.MinimumFreeSpaceGigabytes = 0;
        }

        if (string.IsNullOrEmpty(config.StagingRelativeRoot))
        {
            config.StagingRelativeRoot = "staging";
        }

        if (string.IsNullOrEmpty(config.BackupName))
        {
            config.BackupName = "Backup";
        }

        // A trailing separator makes for some very odd looking combined paths later on.
        config.NetworkShare = config.NetworkShare.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // The same two-line credentials file that K9.OS.NetMap consumes.
        if (!string.IsNullOrEmpty(config.NetworkCredentialsFile) && File.Exists(config.NetworkCredentialsFile))
        {
            string[] lines = FileUtil.GetAllNonEmptyLines(config.NetworkCredentialsFile!);
            if (lines.Length >= 2)
            {
                config.NetworkUsername = lines[0];
                config.NetworkPassword = lines[1];
            }
            else
            {
                Log.WriteLine($"The NetworkCredentialsFile ({config.NetworkCredentialsFile}) did not contain two lines.",
                    ILogOutput.LogType.Warning);
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(config.NetworkMapping))
        {
            // Accept H, H: and H:\ and settle on the drive letter form that net use expects.
            config.NetworkMapping = config.NetworkMapping!.TrimEnd('\\', '/');
            if (!config.NetworkMapping.EndsWith(":"))
            {
                config.NetworkMapping += ":";
            }
            config.NetworkMapping = config.NetworkMapping.ToUpper();
        }

        config.PerforceClientName = config.PerforceClientName + "_" + Environment.MachineName.Replace(" ", "_").ToUpper();
    }

    public bool TryGetBackupTimeOfDay(out TimeSpan timeOfDay)
    {
        if (TimeSpan.TryParse(BackupTimeOfDay, CultureInfo.InvariantCulture, out timeOfDay) &&
            timeOfDay >= TimeSpan.Zero && timeOfDay < TimeSpan.FromDays(1))
        {
            return true;
        }

        timeOfDay = TimeSpan.Zero;
        return false;
    }

    public string GetWorkspaceFolder()
    {
        return Path.Combine(DataRoot, "workspace");
    }

    public string GetStagingFolder()
    {
        return Path.Combine(DataRoot, StagingRelativeRoot);
    }

    public string GetStateFile()
    {
        return Path.Combine(DataRoot, "backup.state.json");
    }

    public string GetBackupSourceFolder()
    {
        return !string.IsNullOrEmpty(BackupSourceFolder)
            ? BackupSourceFolder!
            : Path.Combine(GetWorkspaceFolder(), BackupSourceRelativeRoot);
    }

    public string GetNetworkRoot()
    {
        return string.IsNullOrEmpty(NetworkMapping) ? NetworkShare : NetworkMapping!;
    }

    public string GetNetworkTargetFolder()
    {
        string root = GetNetworkRoot();
        return string.IsNullOrEmpty(NetworkTargetFolder)
            ? root
            : Path.Combine(root, NetworkTargetFolder);
    }

    public string GetArchiveFileName(int changelist)
    {
        return ApplyNameTemplate(
            DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            changelist != -1 ? changelist.ToString() : "unknown") + ".7z";
    }

    // Wildcards stand in for everything but the name, so pruning only ever considers our own archives.
    public string GetArchiveSearchPattern()
    {
        return ApplyNameTemplate("*", "*") + ".7z";
    }

    string ApplyNameTemplate(string date, string changelist)
    {
        return BackupNameTemplate
            .Replace("$Name", BackupName)
            .Replace("$Date", date)
            .Replace("$Machine", Environment.MachineName)
            .Replace("$Changelist", changelist);
    }
}
