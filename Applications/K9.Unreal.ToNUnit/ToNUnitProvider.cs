// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.Unreal.ToNUnit;

public class ToNUnitProvider : ProgramProvider
{
    public string? Source;
    public string? Target;
    public string? Suite = "UAT";

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("SOURCE"))
        {
            Log.WriteLine("A SOURCE file is required (---SOURCE=/my/source/file.json)");
            return false;
        }

        if (!File.Exists(args.GetOverrideArgument("SOURCE")))
        {
            Log.WriteLine($"Unable to find SOURCE @ {args.GetOverrideArgument("SOURCE")}", ILogOutput.LogType.Warning);
            return false;
        }

        if (!args.HasOverrideArgument("TARGET"))
        {
            Log.WriteLine("A TARGET file is required (---SOURCE=/my/target.xml)");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Source = args.GetOverrideArgument("SOURCE");
        Target = args.GetOverrideArgument("TARGET");

        if (args.HasOverrideArgument("SUITE"))
        {
            Suite = args.GetOverrideArgument("SUITE");
        }

        base.ParseArguments(args);
    }

    public override string GetDescription()
    {
        return "Convert Gauntlet JSON reports to NUnit XML reports.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[3];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "An absolute path to a Gauntlet JSON report.");
        lines[1] = new KeyValuePair<string, string>("TARGET", "The absolute path of the NUnit XML to output.");
        lines[2] = new KeyValuePair<string, string>("SUITE", "The name of the suite to use in the NUnit XML report. (Optional: UAT)");

        return lines;
    }

}