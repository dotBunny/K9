// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using K9.Core;
using K9.Core.Extensions;
using K9.Core.Modules;

namespace K9.OS.NetMap;

public class NetMapProvider : ProgramProvider
{
    public string? NetworkUsername;
    public string? NetworkPassword;
    public string NetworkMapping = "H:"; // Default to windows drive
    public string NetworkShare = @"\\192.168.20.21\Horde"; // This is the farms NAS path to the Horde share

    public override string GetDescription()
    {
        return "Provide a mechanism for ensuring network shares are mapped as expected.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[4];

        lines[0] = new KeyValuePair<string, string>("NETWORK-USERNAME",
            "Username for network access to be established.");
        lines[1] = new KeyValuePair<string, string>("NETWORK-PASSWORD",
            "Password for network access to be established.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            lines[2] = new KeyValuePair<string, string>("NETWORK-MAPPING",
                "Drive letter to map the network share to. (Optional: H)");
        }
        else
        {
            lines[2] = new KeyValuePair<string, string>("NETWORK-MAPPING",
                "Path to map the network share to. (Optional: /dev/mapping)");
        }

        lines[3] = new KeyValuePair<string, string>("NETWORK-SHARE",
            @"Network share path to the Horde share. (Optional: \\192.168.20.21\Horde)");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("NETWORK-USERNAME"))
        {
            Log.WriteLine("A NETWORK-USERNAME is required (---NETWORK-USERNAME=myusername)");
            return false;
        }
        if (!args.HasOverrideArgument("NETWORK-PASSWORD"))
        {
            Log.WriteLine("A NETWORK-PASSWORD is required (---NETWORK-PASSWORD=mypassword)");
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
        NetworkUsername = args.GetOverrideArgument("NETWORK-USERNAME");
        NetworkPassword = args.GetOverrideArgument("NETWORK-PASSWORD");

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