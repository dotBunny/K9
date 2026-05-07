// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using K9.Core;
using K9.Core.Extensions;
using K9.Core.Modules;

namespace K9.OS.NetMap;

public class NetMapProvider : ProgramProvider
{
    public string? NetworkUsername;
    public string? NetworkPassword;
    public string NetworkMapping = "H:"; // Default to windows' drive
    public string NetworkShare = @"\\192.168.20.21\Horde"; // This is the farms NAS path to the Horde share

    public override string GetDescription()
    {
        return "Provide a mechanism for ensuring network shares are mapped as expected.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[5];

        lines[0] = new KeyValuePair<string, string>("CREDENTIALS",
            "A path to a file with two lines, first line being the username, second being the password.");
        lines[1] = new KeyValuePair<string, string>("NETWORK-USERNAME",
            "Username for network access to be established.");
        lines[2] = new KeyValuePair<string, string>("NETWORK-PASSWORD",
            "Password for network access to be established.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            lines[3] = new KeyValuePair<string, string>("NETWORK-MAPPING",
                "Drive letter to map the network share to. (Optional: H)");
        }
        else
        {
            lines[4] = new KeyValuePair<string, string>("NETWORK-MAPPING",
                "Path to map the network share to. (Optional: /dev/mapping)");
        }

        lines[5] = new KeyValuePair<string, string>("NETWORK-SHARE",
            @"Network share path to the Horde share. (Optional: \\192.168.20.21\Horde)");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("CREDENTIALS") && !args.HasOverrideArgument("NETWORK-USERNAME"))
        {
            Log.WriteLine("A NETWORK-USERNAME is required (---NETWORK-USERNAME=username) or CREDENTIALS.");
            return false;
        }
        if (!args.HasOverrideArgument("CREDENTIALS") && !args.HasOverrideArgument("NETWORK-PASSWORD"))
        {
            Log.WriteLine("A NETWORK-PASSWORD is required (---NETWORK-PASSWORD=password)");
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (args.HasOverrideArgument("NETWORK-MAPPING"))
            {
                string testDriveLetter = args.GetOverrideArgument("NETWORK-MAPPING");
                if (testDriveLetter.Length != 1)
                {
                    Log.WriteLine("NETWORK-MAPPING must be a single letter (---NETWORK-MAPPING=H)");
                    return false;
                }

                if (testDriveLetter.IsNumeric())
                {
                    Log.WriteLine("NETWORK-MAPPING cannot be a number (---NETWORK-MAPPING=H)");
                    return false;
                }
            }
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("CREDENTIALS") &&
            Path.Exists(args.GetOverrideArgument("CREDENTIALS")))
        {
            int lineIndex = 0;
            string[] lines = File.ReadAllLines(args.GetOverrideArgument("CREDENTIALS"));
            int lineCount = lines.Length;
            for (int i = 0; i < lineCount; i++)
            {
                string line = lines[i].Trim();
                if (line.Length > 0 && lineIndex < 2)
                {
                    switch (lineIndex)
                    {
                        case 0:
                            NetworkUsername = line;
                            lineIndex++;
                            break;
                        case 1:
                            NetworkPassword = line;
                            lineIndex++;
                            break;
                    }
                }
            }
        }
        else
        {
            NetworkUsername = args.GetOverrideArgument("NETWORK-USERNAME");
            NetworkPassword = args.GetOverrideArgument("NETWORK-PASSWORD");
        }


        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (args.HasOverrideArgument("NETWORK-MAPPING"))
            {
                NetworkMapping = args.GetOverrideArgument("NETWORK-MAPPING").ToUpper() + ":";
            }
        }
        else
        {
            NetworkMapping = args.GetOverrideArgument("NETWORK-MAPPING");
        }

        if (args.HasOverrideArgument("NETWORK-SHARE"))
        {
            NetworkShare = args.GetOverrideArgument("NETWORK-SHARE");
        }

        base.ParseArguments(args);
    }
}