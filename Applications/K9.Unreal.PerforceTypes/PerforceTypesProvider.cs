// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;
using K9.Services.Perforce;

namespace K9.Unreal.Types;

public class PerforceTypesProvider : ProgramProvider
{
    public string? WorkspaceRoot;

    public int Changelist;
    public string? TargetDirectory;

    public override string GetDescription()
    {
        return "A tool to ensure the types of files in perforce are correct.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("CHANGELIST", "The changelist to add type changes too. This should already be created.");
        lines[1] = new KeyValuePair<string, string>("TARGET-DIRECTORY", "The path where to recursively look for files needing changes. (Optional: Workspace Root)");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();
        if (workspaceRoot == null)
        {
            Log.WriteLine("Unable to find workspace root.", ILogOutput.LogType.Warning);
            return false;
        }

        if (!args.HasOverrideArgument("CHANGELIST"))
        {
            Log.WriteLine("A changelist must be defined.", ILogOutput.LogType.Warning);
            return false;
        }

        if(!int.TryParse(args.OverrideArguments["CHANGELIST"], out int _))
        {
            Log.WriteLine($"Unable to parse CHANGELIST({args.OverrideArguments["CHANGELIST"]}).", ILogOutput.LogType.Warning);
            return false;
        }

        if (args.HasOverrideArgument("TARGET-DIRECTORY") &&
            !Directory.Exists(args.OverrideArguments["TARGET-DIRECTORY"]))
        {
            Log.WriteLine("Unable to find TARGET-DIRECTORY.", ILogOutput.LogType.Warning);
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        WorkspaceRoot = PerforceUtil.GetWorkspaceRoot();
        TargetDirectory = WorkspaceRoot;

        Changelist = int.Parse(args.OverrideArguments["CHANGELIST"]);

        if (args.HasOverrideArgument("TARGET-DIRECTORY"))
        {
            TargetDirectory = args.OverrideArguments["TARGET-DIRECTORY"];
        }

        base.ParseArguments(args);
    }
}