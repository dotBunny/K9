// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9.Publish.SteamToken;

public class SteamTokenConfig : ProgramConfig
{
    public bool ForceFlag;

    public string? Token;
    public string InstallPackage = @"H:\Steamworks\SDK\161.zip";
    public string InstallLocation = "D:\\Steam";
    public string TokenFolder = @"H:\Steamworks\Tokens";

    public string? AppBuild;
    public int RetryCount = 3;
    public string? TokenTarget;

    string? m_NetworkUsername;
    string? m_NetworkPassword;
    string m_NetworkDrive = "H:";
    string m_NetworkShare = @"\\192.168.20.21\Horde"; // This is the farms NAS path to the Horde share

    // ReSharper disable StringLiteralTypo
    public override void Parse(ArgumentsModule args)
    {
        base.Parse(args);

        // Should we force operations?
        ForceFlag = args.BaseArguments.Contains("FORCE");

        // Network Share Settings
        if (args.HasOverrideArgument("NETWORK-USERNAME"))
        {
            m_NetworkUsername = args.OverrideArguments["NETWORK-USERNAME"];
        }
        if (args.HasOverrideArgument("NETWORK-PASSWORD"))
        {
            m_NetworkPassword = args.OverrideArguments["NETWORK-PASSWORD"];
        }
        if (args.HasOverrideArgument("NETWORK-DRIVE"))
        {
            m_NetworkDrive = args.OverrideArguments["NETWORK-DRIVE"];
        }
        if (args.HasOverrideArgument("NETWORK-SHARE"))
        {
            m_NetworkShare = args.OverrideArguments["NETWORK-SHARE"];
        }

        // We need to early configure the network share if we have a password
        if (!string.IsNullOrEmpty(m_NetworkPassword) && !string.IsNullOrEmpty(m_NetworkUsername) && !Directory.Exists(TokenFolder))
        {
            Log.WriteLine($"Establishing network share {m_NetworkDrive} -> {m_NetworkShare}");
            ProcessUtil.Execute("net", null, $"use {m_NetworkDrive} {m_NetworkShare} /USER:{m_NetworkUsername} {m_NetworkPassword}", null, (processIdentifier, line) =>
            {
                Log.WriteLine($"[{processIdentifier}]\t{line}");
            });
        }

        if (args.HasOverrideArgument("TOKEN-TARGET"))
        {
            TokenTarget = args.OverrideArguments["TOKEN-TARGET"];
        }

        if (args.HasOverrideArgument("TOKEN-FOLDER"))
        {
            TokenFolder = args.OverrideArguments["TOKEN-FOLDER"];
        }

        if (!Directory.Exists(TokenFolder))
        {
            throw (new DirectoryNotFoundException($"Unable to reach the token folder @ {TokenFolder}"));
        }

        if (args.HasOverrideArgument("INSTALL-PACKAGE"))
        {
            InstallPackage = args.OverrideArguments["INSTALL-PACKAGE"];
        }

        if (!File.Exists(InstallPackage))
        {
            throw (new DirectoryNotFoundException($"Unable to reach the install package @ {InstallPackage}"));
        }

        if (args.HasOverrideArgument("INSTALL-LOCATION"))
        {
            InstallLocation = args.OverrideArguments["INSTALL-LOCATION"];
        }

        if (args.HasOverrideArgument("TOKEN"))
        {
            Token = args.OverrideArguments["TOKEN"];
        }

        if (args.HasOverrideArgument("RETRYCOUNT") &&

            int.TryParse(args.OverrideArguments["RETRYCOUNT"], out int count))
        {
            RetryCount = count;
        }

        if (RetryCount < 0)
        {
            throw new Exception("Retry count must not be negative.");
        }

        if (args.HasOverrideArgument("APPBUILD"))
        {
            AppBuild = args.OverrideArguments["APPBUILD"];
            if (!File.Exists(AppBuild))
            {
                throw new FileNotFoundException($"Unable to access {AppBuild}");
            }
        }


        if (string.IsNullOrEmpty(AppBuild))
        {
            throw new Exception("You need to provide an APPBUILD to utilizing the LAUNCH action.");
        }
        // ReSharper restore StringLiteralTypo
    }

}
