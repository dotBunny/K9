// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.CopyFolder;

public class CopyFolderProvider : ProgramProvider
{
    public string? SourceFolder;
    public string? TargetFolder;

    public bool ClearTargetFolder;

    public override string GetDescription()
    {
        return "A simple safe way to copy a folder's contents to another folder.";
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[1];

        lines[0] = new KeyValuePair<string, string>("CLEAR-TARGET", "Should content be deleted from the TARGET folder prior to copying the content?");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The absolute path of the folder to copy.");
        lines[1] = new KeyValuePair<string, string>("TARGET", "The absolute path of the folder to copy the content to.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("SOURCE"))
        {
            if (!Directory.Exists(args.GetOverrideArgument("SOURCE")))
            {
                Log.WriteLine($"Unable to find SOURCE @ {args.GetOverrideArgument("SOURCE")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("A SOURCE folder is required (---SOURCE=/my/source/folder)");
            return false;
        }

        if (!args.HasOverrideArgument("TARGET"))
        {
            Log.WriteLine("A TARGET folder is required (---SOURCE=/my/target)");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        SourceFolder = args.GetOverrideArgument("SOURCE");
        TargetFolder = args.GetOverrideArgument("TARGET");
        ClearTargetFolder = args.HasBaseArgument("CLEAR-TARGET");

        base.ParseArguments(args);
    }
}