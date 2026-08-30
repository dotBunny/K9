// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using K9.Core;
using K9.Core.Utils;

namespace K9.Service.PerforceBackup;

// Persisted beside the workspace so a restart neither double-fires nor skips the daily window.
public class PerforceBackupState
{
    // ReSharper disable PropertyCanBeMadeInitOnly.Global
    public DateTime? LastSuccessUtc { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public string? LastArchive { get; set; }

    // An archive that was built and verified, but could not be copied to the share. Compression can take
    // hours, so a network blip afterwards should not cost us the whole pass.
    public string? PendingArchive { get; set; }
    // ReSharper restore PropertyCanBeMadeInitOnly.Global

    static readonly JsonSerializerOptions k_JsonSettings = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static PerforceBackupState Get(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new PerforceBackupState();
        }

        try
        {
            return JsonSerializer.Deserialize<PerforceBackupState>(File.ReadAllText(filePath), k_JsonSettings) ??
                   new PerforceBackupState();
        }
        catch (Exception e)
        {
            // A corrupt state file must not stop the service, it just costs us one extra backup.
            Log.WriteLine($"Unable to read the state file @ {filePath} ({e.Message}), starting fresh.",
                ILogOutput.LogType.Warning);
            return new PerforceBackupState();
        }
    }

    public void Write(string filePath)
    {
        try
        {
            FileUtil.EnsureFileFolderHierarchyExists(filePath);
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, k_JsonSettings));
        }
        catch (Exception e)
        {
            Log.WriteLine($"Unable to write the state file @ {filePath} ({e.Message}).", ILogOutput.LogType.Error);
        }
    }
}
