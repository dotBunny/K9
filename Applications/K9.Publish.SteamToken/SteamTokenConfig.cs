// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Utils;

namespace K9.Publish.SteamToken;

public class SteamTokenConfig
{
    public bool ForceFlag { get; set; }

    public string? Token { get; set; }
    public string InstallPackage { get; set; } = @"H:\Steamworks\SDK\161.zip";
    public string InstallLocation { get; set; } = "D:\\Steam";
    public string TokenFolder { get; set; }= @"H:\Steamworks\Tokens";

    public string? NetworkUsername { get; set; }
    public string? NetworkPassword { get; set; }
    public string NetworkDrive { get; set; } = "H:";
    public string NetworkShare { get; set; } = @"\\192.168.20.21\Horde"; // This is the farms NAS path to the Horde share

    public string? AppBuild{ get; set; }
    public int RetryCount { get; set; } = 3;
    public string? TokenTarget { get; set; }

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
            config.NetworkUsername = framework.Arguments.OverrideArguments["NETWORK-USERNAME"];
        }
        if (framework.Arguments.HasOverrideArgument("NETWORK-PASSWORD"))
        {
            config.NetworkPassword = framework.Arguments.OverrideArguments["NETWORK-PASSWORD"];
        }
        if (framework.Arguments.HasOverrideArgument("NETWORK-DRIVE"))
        {
            config.NetworkDrive = framework.Arguments.OverrideArguments["NETWORK-DRIVE"];
        }
        if (framework.Arguments.HasOverrideArgument("NETWORK-SHARE"))
        {
            config.NetworkShare = framework.Arguments.OverrideArguments["NETWORK-SHARE"];
        }

        // We need to early configure the network share if we have a password
        if (!string.IsNullOrEmpty(config.NetworkPassword) && !string.IsNullOrEmpty(config.NetworkUsername) && !Directory.Exists(config.TokenFolder))
        {
            Log.WriteLine($"Establishing network share {config.NetworkDrive} -> {config.NetworkShare}");
            ProcessUtil.Execute("net", null, $"use {config.NetworkDrive} {config.NetworkShare} /USER:{config.NetworkUsername} {config.NetworkPassword}", null, (processIdentifier, line) =>
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
