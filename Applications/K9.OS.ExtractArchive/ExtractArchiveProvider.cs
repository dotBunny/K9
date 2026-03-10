// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.ExtractArchive;

public class ExtractArchiveProvider : ProgramProvider
{
    public string? Source;
    public string? TargetFolder;
    public bool CheckExists = false;

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("SOURCE"))
        {
            Log.WriteLine("A SOURCE file is required (---SOURCE=/my/source/file)");
            return false;
        }

        if (!File.Exists(args.GetOverrideArgument("SOURCE")))
        {
            Log.WriteLine($"Unable to find SOURCE @ {args.GetOverrideArgument("SOURCE")}", ILogOutput.LogType.Warning);
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Source = args.GetOverrideArgument("SOURCE");
        if (args.HasOverrideArgument("TARGET-FOLDER"))
        {
            TargetFolder = args.GetOverrideArgument("TARGET");
        }

        CheckExists = args.HasBaseArgument("CHECK-EXISTS");

        base.ParseArguments(args);
    }

    public override string GetDescription()
    {
        return "Extract an archive.";
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[1];

        lines[0] = new KeyValuePair<string, string>("CHECK-EXISTS", "Should we check if the target folder exists and has content?");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The absolute path to the archive.");
        lines[1] = new KeyValuePair<string, string>("TARGET-FOLDER", "The location where to extract the archive.");

        return lines;
    }
}