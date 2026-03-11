// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using K9.Core;

namespace K9.OS.SetEnvironmentVariable;

public class SetEnvironmentVariableProvider : ProgramProvider
{
    public string? UserTarget;
    public string? SystemTarget;

    public string? UserKeys;
    public string? UserValues;

    public string? SystemKeys;
    public string? SystemValues;

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
}