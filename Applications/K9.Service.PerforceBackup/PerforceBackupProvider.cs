// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.Service.PerforceBackup;

public class PerforceBackupProvider : ProgramProvider
{
    // Base arguments the framework or this application owns, they are never a config path.
    static readonly string[] k_ReservedArguments =
        ["HELP", "NO-PAUSE", "QUIET", "ELEVATION-CHECK", "RUN-NOW", "NO-UPLOAD"];

    public PerforceBackupConfig? Config;

    public override bool IsValid(ArgumentsModule args)
    {
        string? jsonPath = GetConfigPath(args);

        // No argument, use the config beside the assembly.
        if (jsonPath == null)
        {
            return PerforceBackupConfig.Get().IsValid() && base.IsValid(args);
        }

        if (!File.Exists(jsonPath))
        {
            Log.WriteLine($"Unable to find provided config file @ {jsonPath}, returning empty data.",
                ILogOutput.LogType.Warning);
            return false;
        }

        return PerforceBackupConfig.Get(jsonPath).IsValid() && base.IsValid(args);
    }

    public override string GetDescription()
    {
        return "Keeps a Perforce workspace synced and archives a folder to a network share daily.";
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Config = PerforceBackupConfig.Get(GetConfigPath(args));
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[1];

        lines[0] = new KeyValuePair<string, string>("DATA-DIR",
            "Override the DataRoot defined in the configuration file. (Optional)");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("RUN-NOW",
            "Run a backup immediately on the first cycle instead of waiting for the scheduled time.");
        lines[1] = new KeyValuePair<string, string>("NO-UPLOAD",
            "Create and verify the archive, but leave it in staging instead of copying it to the share.");

        return lines;
    }

    static string? GetConfigPath(ArgumentsModule args)
    {
        string firstArgument = args.GetFirstArgument();
        if (string.IsNullOrEmpty(firstArgument))
        {
            return null;
        }

        return Array.IndexOf(k_ReservedArguments, firstArgument.ToUpper()) != -1 ? null : firstArgument;
    }
}
