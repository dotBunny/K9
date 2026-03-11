// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.CopyFile;

public class CopyFileProvider : ProgramProvider
{
    public string? SourcePath;
    public string? TargetFolder;
    public string? Target;
    public bool CheckExists = true;
    public bool Extract;

    public bool TargetFile;

    public override string GetDescription()
    {
        return "Copy a file to target folder, optionally extracting it.";
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("CHECK-EXISTS", "Should we check if the target folder exists and has content?");
        lines[1] = new KeyValuePair<string, string>("EXTRACT", "If the SOURCE is an archive, should we extract it to the TARGET folder?");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[3];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The absolute path of the file to copy.");
        lines[1] = new KeyValuePair<string, string>("TARGET-FOLDER", "The absolute path of the folder to copy the file to.");
        lines[2] = new KeyValuePair<string, string>("TARGET", "The absolute path where to copy the file.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("SOURCE"))
        {
            if (!File.Exists(args.GetOverrideArgument("SOURCE")))
            {
                Log.WriteLine($"Unable to find SOURCE @ {args.GetOverrideArgument("SOURCE")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("A SOURCE file is required (---SOURCE=/my/source/file)");
            return false;
        }

        if (!args.HasOverrideArgument("TARGET") && !args.HasOverrideArgument("TARGET-FOLDER"))
        {
            Log.WriteLine("A TARGET path or TARGET-FOLDER is required.");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        SourcePath = args.GetOverrideArgument("SOURCE");
        if (args.HasOverrideArgument("TARGET"))
        {
            Target = args.GetOverrideArgument("TARGET");
        }

        if (args.HasOverrideArgument("TARGET-FOLDER"))
        {
            TargetFolder = args.GetOverrideArgument("TARGET");
        }

        TargetFile = !string.IsNullOrEmpty(Target);
        Extract = args.HasBaseArgument("EXTRACT");
        CheckExists = args.HasBaseArgument("CHECK-EXISTS");

        base.ParseArguments(args);
    }
}