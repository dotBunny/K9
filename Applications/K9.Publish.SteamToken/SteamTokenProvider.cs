// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9.Publish.SteamToken;

public class SteamTokenProvider : ProgramProvider
{
    public bool ForceFlag;

    public string? Token;
    public string InstallPackage = @"H:\Steamworks\SDK\161.zip";
    public string InstallLocation = "D:\\Steam";
    public string TokenFolder = @"H:\Steamworks\Tokens";

    // ReSharper disable once UnassignedField.Global
    public string? AppBuild;
    public int RetryCount = 3;
    public string? TokenTarget;

    string? m_NetworkUsername;
    string? m_NetworkPassword;
    string m_NetworkDrive = "H:";
    string m_NetworkShare = @"\\192.168.20.21\Horde"; // This is the farms NAS path to the Horde share

    public override string GetDescription()
    {
        return
            "An application to check out and check-in the token used for SteamGuard uploads. This allows for the token to change and be updated for other agents to access.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[11];

        lines[0] = new KeyValuePair<string, string>("NETWORK-USERNAME", "Username for network access to be established. (Optional: required to map a network share.)");
        lines[1] = new KeyValuePair<string, string>("NETWORK-PASSWORD", "Password for network access to be established. (Optional: required to map a network share.)");
        lines[2] = new KeyValuePair<string, string>("NETWORK-DRIVE", "Drive letter to map the network share to. (Optional: H:)");
        lines[3] = new KeyValuePair<string, string>("NETWORK-SHARE", @"Network share path to the Horde share. (Optional: \\192.168.20.21\Horde)");

        lines[4] = new KeyValuePair<string, string>("TOKEN-TARGET", "The absolute path to write the token file for SteamGuard to.");
        lines[5] = new KeyValuePair<string, string>("TOKEN-FOLDER", @"Where the SteamGuard tokens are stored remotely. (Optional: H:\Steamworks\Tokens)");

        lines[6] = new KeyValuePair<string, string>("INSTALL-LOCATION", @"Where should the SDK be installed/extracted? (Optional: D:\Steam)");
        lines[7] = new KeyValuePair<string, string>("INSTALL-PACKAGE", @"The default Steam SDK to uncompress. (Optional: H:\Steamworks\SDK\161.zip) ");

        lines[8] = new KeyValuePair<string, string>("TOKEN", "Looks to force the use of a specifically named token / user.");
        // ReSharper disable StringLiteralTypo
        lines[9] = new KeyValuePair<string, string>("RETRYCOUNT", "Sometimes, Steam randomly has some odd failures about network connectivity issues. This will retry the operation a few times before giving up. (Optional: 3)");
        lines[10] = new KeyValuePair<string, string>("APPBUILD", "The absolute path to the VDF file to use to facilitate the SteamCMD upload.");
        // ReSharper restore StringLiteralTypo

        return lines;
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[1];

        lines[0] = new KeyValuePair<string, string>("FORCE", "Forces the token to be taken from the hotelling, regardless of the pre-existing lock.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("TOKEN-FOLDER") && !Directory.Exists(args.GetOverrideArgument("TOKEN-FOLDER")))
        {
            Log.WriteLine($"Unable to reach the token folder @ {args.GetOverrideArgument("TOKEN-FOLDER")}", ILogOutput.LogType.Warning);
            return false;
        }

        if (args.HasOverrideArgument("INSTALL-PACKAGE") && !File.Exists(args.GetOverrideArgument("INSTALL-PACKAGE")))
        {
            Log.WriteLine($"Unable to reach the install package @ {InstallPackage}", ILogOutput.LogType.Warning);
            return false;
        }

        // ReSharper disable StringLiteralTypo
        if (args.HasOverrideArgument("RETRYCOUNT") && int.TryParse(args.GetOverrideArgument("RETRYCOUNT"), out int count))
        {
            if (count < 0)
            {
                Log.WriteLine($"Retry count must be greater than or equal to 0.", ILogOutput.LogType.Warning);
                return false;
            }
        }

        if (args.HasOverrideArgument("APPBUILD"))
        {
            if (!File.Exists(args.GetOverrideArgument("APPBUILD")))
            {
                Log.WriteLine($"Unable to reach the app build @ {args.GetOverrideArgument("APPBUILD")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("You need to provide an APPBUILD to utilizing the LAUNCH action.", ILogOutput.LogType.Warning);
            return false;
        }

        if (!args.HasOverrideArgument("TOKEN-TARGET"))
        {
            Log.WriteLine("You need to provide a TOKEN-TARGET to write the token to.", ILogOutput.LogType.Warning);
            return false;
        }

        return base.IsValid(args);
    }

    // ReSharper disable StringLiteralTypo
    public override void ParseArguments(ArgumentsModule args)
    {
        base.ParseArguments(args);

        // Should we force operations?
        ForceFlag = args.HasBaseArgument("FORCE");

        // Network Share Settings
        if (args.HasOverrideArgument("NETWORK-USERNAME"))
        {
            m_NetworkUsername = args.GetOverrideArgument("NETWORK-USERNAME");
        }
        if (args.HasOverrideArgument("NETWORK-PASSWORD"))
        {
            m_NetworkPassword = args.GetOverrideArgument("NETWORK-PASSWORD");
        }
        if (args.HasOverrideArgument("NETWORK-DRIVE"))
        {
            m_NetworkDrive = args.GetOverrideArgument("NETWORK-DRIVE");
        }
        if (args.HasOverrideArgument("NETWORK-SHARE"))
        {
            m_NetworkShare = args.GetOverrideArgument("NETWORK-SHARE");
        }

        if (args.HasOverrideArgument("TOKEN-FOLDER"))
        {
            TokenFolder = args.GetOverrideArgument("TOKEN-FOLDER");
        }

        if (args.HasOverrideArgument("TOKEN-TARGET"))
        {
            TokenTarget = args.GetOverrideArgument("TOKEN-TARGET");
        }

        if (args.HasOverrideArgument("INSTALL-PACKAGE"))
        {
            InstallPackage = args.GetOverrideArgument("INSTALL-PACKAGE");
        }

        if (args.HasOverrideArgument("INSTALL-LOCATION"))
        {
            InstallLocation = args.GetOverrideArgument("INSTALL-LOCATION");
        }

        if (args.HasOverrideArgument("TOKEN"))
        {
            Token = args.GetOverrideArgument("TOKEN");
        }

        if (args.HasOverrideArgument("RETRYCOUNT"))
        {
            RetryCount = int.Parse(args.GetOverrideArgument("RETRYCOUNT"));
        }
    }
    // ReSharper restore StringLiteralTypo


    public void EnsureNetworkPath()
    {
        // We need to early configure the network share if we have a password
        if (!string.IsNullOrEmpty(m_NetworkPassword) && !string.IsNullOrEmpty(m_NetworkUsername) && !Directory.Exists(TokenFolder))
        {
            Log.WriteLine($"Establishing network share {m_NetworkDrive} -> {m_NetworkShare}");
            ProcessLogRedirect logRedirect = new();
            ProcessUtil.Execute("net", null,
                $"use {m_NetworkDrive} {m_NetworkShare} /USER:{m_NetworkUsername} {m_NetworkPassword}", null,
                logRedirect.GetAction());
        }

    }

}
