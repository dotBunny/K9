// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9;

public class K9Provider : ProgramProvider
{
    public string? WorkspaceRoot;

    public override string GetDescription()
    {
        return "A tool to execute pre-defined tasks.";
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

    public override bool IsHelpOverride()
    {
        return true;
    }
}