// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9.Workspace.Setup;

public class SetupProvider : ProgramProvider
{
    public string? WorkspaceRoot;

    public override string GetDescription()
    {
        return "This application sets up a user's workspace just how we want it.";
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