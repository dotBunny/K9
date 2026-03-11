// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.FileReplacer;

public class FileReplacerProvider : ProgramProvider
{
    public string? SourceFile;
    public string? TargetFile;
    public readonly Dictionary<string, string> Replaces = new();

    public override string GetDescription()
    {
        return "A tool for replacing content in a file in one-shot.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[4];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The absolute path of the file to read the content to be replaced from.");
        lines[1] = new KeyValuePair<string, string>("TARGET", "The absolute path of the file to write the replaced content to. (Optional: Uses the SOURCE file as TARGET)");
        lines[2] = new KeyValuePair<string, string>("KEY", "A comma delimited list of strings to replace.");
        lines[3] = new KeyValuePair<string, string>("VALUE", "A comma delimited list of values to replace the identified keys with.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        // Check SOURCE
        if (args.HasOverrideArgument("SOURCE"))
        {
            if (!File.Exists(args.GetOverrideArgument("SOURCE")))
            {
                Log.WriteLine($"Unable to find source {args.GetOverrideArgument("SOURCE")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("A SOURCE file is required (---SOURCE=/my/input)");
            return false;
        }

        // Check TARGET
        if (args.HasOverrideArgument("TARGET"))
        {
            if (!File.Exists(args.GetOverrideArgument("TARGET")))
            {
                Log.WriteLine($"Unable to find target {args.GetOverrideArgument("TARGET")}", ILogOutput.LogType.Warning);
                return false;
            }
        }

        // Check KEY & VALUE
        if (args.HasOverrideArgument("KEY") && args.HasOverrideArgument("VALUE"))
        {
            string[] keys = args.GetOverrideArgument("KEY").Split(",", StringSplitOptions.RemoveEmptyEntries);
            string[] values = args.GetOverrideArgument("VALUE").Split(",", StringSplitOptions.RemoveEmptyEntries);

            if (keys.Length != values.Length)
            {
                Log.WriteLine($"The number of KEY({keys.Length}) does match the number of VALUE({values.Length}).", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("Both KEY and VALUE are necessary (---KEY=a,b,c ---VALUE=1,2,3)", ILogOutput.LogType.Warning);
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        base.ParseArguments(args);

        SourceFile = args.GetOverrideArgument("SOURCE");
        TargetFile = !args.HasOverrideArgument("TARGET") ? SourceFile : args.GetOverrideArgument("TARGET");

        string[] keys = args.GetOverrideArgument("KEY").Split(",", StringSplitOptions.RemoveEmptyEntries);
        string[] values = args.GetOverrideArgument("VALUE").Split(",", StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < keys.Length; i++)
        {
            Replaces.Add(keys[i], values[i]);
        }
    }
}