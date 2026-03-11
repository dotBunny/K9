// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.SetEnvironmentVariable;

public class SetEnvironmentVariableProvider : ProgramProvider
{
    public string? UserTarget;
    public string? SystemTarget;

    public string[]? UserKeys;
    public string[]? UserValues;

    public string[]? SystemKeys;
    public string[]? SystemValues;

    public override string GetDescription()
    {
        return "Set different environment variables based on inputs.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[6];

        lines[0] = new KeyValuePair<string, string>("USER-KEYS", "A comma-delimited list of user-level environmental variables' keys.");
        lines[1] = new KeyValuePair<string, string>("USER-VALUES", "A comma-delimited list of user-level environmental variables' values.");
        lines[2] = new KeyValuePair<string, string>("USER-TARGET", "The path to a file storing a comma-separated list of user-level environmental variables.");
        lines[3] = new KeyValuePair<string, string>("SYSTEM-KEYS", "A comma-delimited list of system-wide environmental variables' keys.");
        lines[4] = new KeyValuePair<string, string>("SYSTEM-VALUES", "A comma-delimited list of system-wide environmental variables' values.");
        lines[5] = new KeyValuePair<string, string>("SYSTEM-TARGET", "The path to a file storing a comma-separated list of system-wide environmental variables.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        bool hasSomething = false;

        if (args.HasOverrideArgument("USER-KEYS") && args.HasOverrideArgument("USER-VALUES"))
        {
            hasSomething = true;
            string[] keys = args.GetOverrideArgument("USER-KEYS").Split(',', StringSplitOptions.RemoveEmptyEntries);
            string[] values = args.GetOverrideArgument("USER-VALUES").Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (keys.Length != values.Length)
            {
                Log.WriteLine("User-level keys and values must be the same length.", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else if(args.HasOverrideArgument("USER-KEYS") && !args.HasOverrideArgument("USER-VALUES"))
        {
            Log.WriteLine("User-level values must be provided with User-level keys.", ILogOutput.LogType.Warning);
            return false;
        }
        else if (args.HasOverrideArgument("USER-VALUES") && !args.HasOverrideArgument("USER-KEYS"))
        {
            Log.WriteLine("User-level keys must be provided with User-level values.", ILogOutput.LogType.Warning);
            return false;
        }

        if(args.HasOverrideArgument("USER-TARGET"))
        {
            hasSomething = true;
            if (!System.IO.File.Exists(args.GetOverrideArgument("USER-TARGET")))
            {
                Log.WriteLine("User-level target file cannot be found.", ILogOutput.LogType.Warning);
                return false;
            }
        }

        if (args.HasOverrideArgument("SYSTEM-KEYS") && args.HasOverrideArgument("SYSTEM-VALUES"))
        {
            hasSomething = true;
            string[] keys = args.GetOverrideArgument("SYSTEM-KEYS").Split(',', StringSplitOptions.RemoveEmptyEntries);
            string[] values = args.GetOverrideArgument("SYSTEM-VALUES").Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (keys.Length != values.Length)
            {
                Log.WriteLine("System-wide keys and values must be the same length.", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else if(args.HasOverrideArgument("SYSTEM-KEYS") && !args.HasOverrideArgument("SYSTEM-VALUES"))
        {
            Log.WriteLine("System-wide values must be provided with system-wide keys.", ILogOutput.LogType.Warning);
            return false;
        }
        else if (args.HasOverrideArgument("SYSTEM-VALUES") && !args.HasOverrideArgument("SYSTEM-KEYS"))
        {
            Log.WriteLine("System-wide keys must be provided with system-wide values.", ILogOutput.LogType.Warning);
            return false;
        }

        if(args.HasOverrideArgument("SYSTEM-TARGET"))
        {
            hasSomething = true;
            if (!System.IO.File.Exists(args.GetOverrideArgument("SYSTEM-TARGET")))
            {
                Log.WriteLine("System-wide target file cannot be found.", ILogOutput.LogType.Warning);
                return false;
            }
        }


        if (!hasSomething)
        {
            Log.WriteLine("No valid operation to perform.", ILogOutput.LogType.Warning);
        }
        return hasSomething;
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("USER-TARGET"))
        {
            UserTarget = args.GetOverrideArgument("USER-TARGET");
        }

        if (args.HasOverrideArgument("SYSTEM-TARGET"))
        {
            SystemTarget = args.GetOverrideArgument("SYSTEM-TARGET");
        }

        if (args.HasOverrideArgument("USER-KEYS") && args.HasOverrideArgument("USER-VALUES"))
        {
            UserKeys = args.GetOverrideArgument("USER-KEYS").Split(',', StringSplitOptions.RemoveEmptyEntries);
            UserValues = args.GetOverrideArgument("USER-VALUES").Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        if (args.HasOverrideArgument("SYSTEM-KEYS") && args.HasOverrideArgument("SYSTEM-VALUES"))
        {
            SystemKeys = args.GetOverrideArgument("SYSTEM-KEYS").Split(',', StringSplitOptions.RemoveEmptyEntries);
            SystemValues = args.GetOverrideArgument("SYSTEM-VALUES").Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        base.ParseArguments(args);
    }
}