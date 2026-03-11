// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.DeleteFolder;

public class DeleteFolderProvider : ProgramProvider
{
    public string? TargetFolder;

    public override string GetDescription()
    {
        return "Deletes a folder, with no nonsense.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[1];

        lines[0] = new KeyValuePair<string, string>("TARGET", "The absolute path of the folder to delete.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("TARGET"))
        {
            if (!Directory.Exists(args.GetOverrideArgument("TARGET")))
            {
                Log.WriteLine($"Unable to find TARGET @ {args.GetOverrideArgument("TARGET")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("A TARGET folder is required (---TARGET=/my/folder)");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        TargetFolder = args.GetOverrideArgument("TARGET");

        base.ParseArguments(args);
    }
}