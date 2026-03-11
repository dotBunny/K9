// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.Wrapper;

public class WrapperProvider : ProgramProvider
{
    public string? Command;
    public string? Arguments;

    public readonly Dictionary<string, string> Replacements = new();

    public bool ReplaceWarnings;
    public bool ReplaceErrors;

    public override string GetDescription()
    {
        return "Wrap execution of applications to control error handling and logging.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[4];

        lines[0] = new KeyValuePair<string, string>("COMMAND", "The absolute path to the command/executable to be run.");
        lines[1] = new KeyValuePair<string, string>("ARGUMENTS", "Arguments to be passed to the executed command. (Optional)");

        lines[2] = new KeyValuePair<string, string>("REPLACE-KEY", "A comma delimited list of strings to replace. (Optional)");
        lines[3] = new KeyValuePair<string, string>("REPLACE-VALUE", "A comma delimited list of values to replace the identified keys with. (Optional)");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("REPLACE-ERRORS", "Replace instances of ERROR/Error/error with [[BAD]].");
        lines[1] = new KeyValuePair<string, string>("REPLACE-WARNINGS", "Replace instances of WARNING/Warning/warning with [[ALERT]].");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("COMMAND"))
        {
            Log.WriteLine("A COMMAND must be defined (---COMMAND=echo");
            return false;
        }

        if (args.HasOverrideArgument("REPLACE-KEY") && args.HasOverrideArgument("REPLACE-VALUE"))
        {
            string[] keys = args.GetOverrideArgument("REPLACE-KEY").Split(",", StringSplitOptions.RemoveEmptyEntries);
            string[] values = args.GetOverrideArgument("REPLACE-VALUE").Split(",", StringSplitOptions.RemoveEmptyEntries);

            if (keys.Length != values.Length)
            {
                Log.WriteLine($"The number of REPLACE-KEY({keys.Length}) does match the number of REPLACE-VALUE({values.Length}).", ILogOutput.LogType.Warning);
                return false;
            }
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Command = args.GetOverrideArgument("COMMAND");
        if (args.HasOverrideArgument("ARGUMENTS"))
        {
            Arguments = args.GetOverrideArgument("ARGUMENTS");
        }

        if (args.HasBaseArgument("REPLACE-ERRORS"))
        {
            ReplaceErrors = true;
        }
        if (args.HasBaseArgument("REPLACE-WARNINGS"))
        {
            ReplaceWarnings = true;
        }

        string[] keys = args.GetOverrideArgument("REPLACE-KEY").Split(",", StringSplitOptions.RemoveEmptyEntries);
        string[] values = args.GetOverrideArgument("REPLACE-VALUE").Split(",", StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < keys.Length; i++)
        {
            Replacements.Add(keys[i], values[i]);
        }
    }
}