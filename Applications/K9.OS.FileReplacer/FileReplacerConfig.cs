// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.FileReplacer;

public class FileReplacerConfig : ProgramConfig
{
    public string? SourceFile;
    public string? TargetFile;
    public readonly Dictionary<string, string> Replaces = new();

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[4];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The absolute path of the file to read the content to be replaced from.");
        lines[1] = new KeyValuePair<string, string>("TARGET", "The absolute path of the file to write the replaced content too, this can be the same as the SOURCE.");
        lines[2] = new KeyValuePair<string, string>("KEY", "A comma delimited list of strings to replace.");
        lines[3] = new KeyValuePair<string, string>("VALUE", "A comma delimited list of values to replace the identified keys with.");
        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {

        // Check SOURCE
        if (args.HasOverrideArgument("SOURCE"))
        {
            if (!File.Exists(SourceFile))
            {
                Log.WriteLine($"Unable to find source {SourceFile}");
                return false;
            }
        }
        else
        {
            Log.WriteLine("A SOURCE file is required (---SOURCE=/my/input)");
            return false;
        }

        // Check TARGET

        // Check KEY

        // Check VALUE

        return true;
    }

    public override void Parse(ArgumentsModule args)
    {
        base.Parse(args);

        // HANDLE SOURCE
        if (args.HasOverrideArgument("SOURCE"))
        {
            SourceFile = args.OverrideArguments["SOURCE"];
            if (!File.Exists(SourceFile))
            {
                throw (new FileNotFoundException($"Unable to find source {SourceFile}"));
            }
        }
        else
        {
            throw (new Exception("A SOURCE file is required (---SOURCE=/my/input)"));
        }

        // Handle TARGET
        if (args.HasOverrideArgument("TARGET"))
        {
            TargetFile = args.OverrideArguments["TARGET"];
        }
        else
        {
            throw (new Exception("A TARGET file is required (---TARGET=/my/output)"));
        }

        // Build Replacement List
        if (args.HasOverrideArgument("KEY") && args.HasOverrideArgument("VALUE"))
        {
            string[] keys = args.OverrideArguments["KEY"].Split(",", StringSplitOptions.RemoveEmptyEntries);
            string[] values = args.OverrideArguments["VALUE"].Split(",", StringSplitOptions.RemoveEmptyEntries);

            if (keys.Length != values.Length)
            {
                throw (new Exception($"The number of KEYS({keys.Length}) does match the number of VALUES({values.Length})."));
            }

            for (int i = 0; i < keys.Length; i++)
            {
                Replaces.Add(keys[i], values[i]);
            }
        }
        else
        {
            throw (new Exception("Both KEY and VALUE are necessary (---KEY=a,b,c ---VALUE=1,2,3)"));
        }

    }
}