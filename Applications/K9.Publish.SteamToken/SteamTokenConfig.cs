// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.Publish.SteamToken;

public class SteamTokenConfig
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
    public static SteamTokenConfig Get(ConsoleApplication framework)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        SteamTokenConfig config = new();

        // Should we force operations?
        config.ForceFlag = framework.Arguments.BaseArguments.Contains("FORCE");

        // Network Share Settings
        if (framework.Arguments.HasOverrideArgument("NETWORK-USERNAME"))
        {
            config.m_NetworkUsername = framework.Arguments.OverrideArguments["NETWORK-USERNAME"];
        }
        if (framework.Arguments.HasOverrideArgument("NETWORK-PASSWORD"))
        {
            config.m_NetworkPassword = framework.Arguments.OverrideArguments["NETWORK-PASSWORD"];
        }
        if (framework.Arguments.HasOverrideArgument("NETWORK-DRIVE"))
        {
            config.m_NetworkDrive = framework.Arguments.OverrideArguments["NETWORK-DRIVE"];
        }
        if (framework.Arguments.HasOverrideArgument("NETWORK-SHARE"))
        {
            config.m_NetworkShare = framework.Arguments.OverrideArguments["NETWORK-SHARE"];
        }

        // We need to early configure the network share if we have a password
        if (!string.IsNullOrEmpty(config.m_NetworkPassword) && !string.IsNullOrEmpty(config.m_NetworkUsername) && !Directory.Exists(config.TokenFolder))
        {
            Log.WriteLine($"Establishing network share {config.m_NetworkDrive} -> {config.m_NetworkShare}");
            ProcessUtil.Execute("net", null, $"use {config.m_NetworkDrive} {config.m_NetworkShare} /USER:{config.m_NetworkUsername} {config.m_NetworkPassword}", null, (processIdentifier, line) =>
            {
                Log.WriteLine($"[{processIdentifier}]\t{line}");
            });
        }

        if (framework.Arguments.HasOverrideArgument("TOKEN-TARGET"))
        {
            config.TokenTarget = framework.Arguments.OverrideArguments["TOKEN-TARGET"];
        }

        if (framework.Arguments.HasOverrideArgument("TOKEN-FOLDER"))
        {
            config.TokenFolder = framework.Arguments.OverrideArguments["TOKEN-FOLDER"];
        }

        if (!Directory.Exists(config.TokenFolder))
        {
            throw (new DirectoryNotFoundException($"Unable to reach the token folder @ {config.TokenFolder}"));
        }

        if (framework.Arguments.HasOverrideArgument("INSTALL-PACKAGE"))
        {
            config.InstallPackage = framework.Arguments.OverrideArguments["INSTALL-PACKAGE"];
        }

        if (!File.Exists(config.InstallPackage))
        {
            throw (new DirectoryNotFoundException($"Unable to reach the install package @ {config.InstallPackage}"));
        }

        if (framework.Arguments.HasOverrideArgument("INSTALL-LOCATION"))
        {
            config.InstallLocation = framework.Arguments.OverrideArguments["INSTALL-LOCATION"];
        }

        if (framework.Arguments.HasOverrideArgument("TOKEN"))
        {
            config.Token = framework.Arguments.OverrideArguments["TOKEN"];
        }

        if (framework.Arguments.HasOverrideArgument("RETRYCOUNT") &&

            int.TryParse(framework.Arguments.OverrideArguments["RETRYCOUNT"], out int count))
        {
            config.RetryCount = count;
        }

        if (config.RetryCount < 0)
        {
            throw new Exception("Retry count must not be negative.");
        }

        if (framework.Arguments.HasOverrideArgument("APPBUILD"))
        {
            config.AppBuild = framework.Arguments.OverrideArguments["APPBUILD"];
            if (!File.Exists(config.AppBuild))
            {
                throw new FileNotFoundException($"Unable to access {config.AppBuild}");
            }
        }

        if (string.IsNullOrEmpty(config.AppBuild))
        {
            throw new Exception("You need to provide an APPBUILD to utilizing the LAUNCH action.");
        }

        return config;
    }
    // ReSharper restore StringLiteralTypo
}
