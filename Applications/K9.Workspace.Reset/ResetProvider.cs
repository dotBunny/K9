// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using K9.Core;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9.Workspace.Reset;

public class ResetProvider : ProgramProvider
{
    public string? WorkspaceRoot;

    public override string GetDescription()
    {
        return "A tool for resetting the state of specific areas of the project.";
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("PROJECT", "Delete project `Intermediate` and `Binaries` folders.");
        lines[1] = new KeyValuePair<string, string>("PROJECT-PLUGINS", "Delete the `Intermediate` and `Binaries` folders of all plugins in projects.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        string? workspaceRoot = WorkspaceUtil.GetWorkspaceRoot();
        if (workspaceRoot == null)
        {
            Log.WriteLine("Unable to find workspace root.", ILogOutput.LogType.Warning);
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        WorkspaceRoot = WorkspaceUtil.GetWorkspaceRoot();
        base.ParseArguments(args);
    }
}