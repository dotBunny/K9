// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Threading;
using K9.Core;
using K9.Core.IO;
using K9.Core.Utils;

namespace K9.Publish.SteamToken;

internal static class Application
{
    static FileLock? s_Token;
    static string? s_TokenUsername;

    static bool CheckoutToken(SteamTokenConfig config)
    {
        // Get token list / lock
        string[] foundTokens = Directory.GetFiles(config.TokenFolder, "*.vdf");
        if (foundTokens.Length <= 0)
        {
            throw new Exception($"Unable to find tokens (*.vdf) at {config.TokenFolder}.");
        }
        Log.WriteLine($"Found {foundTokens.Length} Tokens In Pool.");

        // Handle Specific Target
        if (config.Token != null)
        {
            s_Token = new FileLock(Path.Combine(config.TokenFolder, config.Token + ".vdf"));
            s_Token.Lock(config.ForceFlag);
            if (!s_Token.HasLock())
            {

                if (!s_Token.SafeLock())
                {
                    throw new Exception($"Was unable to acquire lock to {config.Token}");
                }
            }
        }
        if (s_Token == null)
        {
            foreach (string t in foundTokens)
            {
                // We don't allow force when we don't have a target
                s_Token = new FileLock(t);
                if (s_Token.Lock())
                {
                    break;
                }
                s_Token = null;
            }
        }


        if (s_Token == null)
        {
            return false;
        }

        // We need to ensure the folder we are writing exists as it might be a brand-new installation
#pragma warning disable CS8604 // Possible null reference argument.
        FileUtil.EnsureFileFolderHierarchyExists(config.TokenTarget);
#pragma warning restore CS8604 // Possible null reference argument.

        File.Copy(s_Token.FilePath, config.TokenTarget, true);

        s_TokenUsername = Path.GetFileNameWithoutExtension(s_Token.FilePath);

        Log.WriteLine($"Checked out {s_Token.FilePath} to {config.TokenTarget} for user {s_TokenUsername}.");

        return true;
    }
    static bool CheckinToken(SteamTokenConfig config)
    {

        if (s_Token == null)
        {
            throw new Exception("Attempting to check-in a token that is null.");
        }
        if (s_Token.HasLock())
        {
            if (config.TokenTarget != null)
            {
                Log.WriteLine($"Returned {config.TokenTarget} to {s_Token.FilePath}.");

                // Copy File
                File.Copy(config.TokenTarget, s_Token.FilePath, true);
            }
            s_Token.Unlock();
            return true;
        }
        return false;
    }

    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {
            // ReSharper disable once StringLiteralTypo
            DefaultLogCategory = "PUBLISH.STEAMTOKEN",
            LogOutputs = [new Core.Loggers.ConsoleLogOutput()]
        });

        try
        {
            SteamTokenConfig config = SteamTokenConfig.Get(framework);

            // Check for existing installation
            if (!Directory.Exists(Path.Combine(config.InstallLocation, "sdk")))
            {
                // We don't have an existing version installation we need to grab the package
                Log.WriteLine($"Installing Steamworks SDK to {config.InstallLocation}.");
                System.IO.Compression.ZipFile.ExtractToDirectory(config.InstallPackage, config.InstallLocation);
            }

            // ReSharper disable StringLiteralTypo
            string steamCmd = Path.Combine(config.InstallLocation, "sdk", "tools", "ContentBuilder", "builder", "steamcmd.exe");
            // ReSharper restore StringLiteralTypo


            if (!File.Exists(steamCmd))
            {
                throw new Exception("Unable to find SteamCMD");
            }
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string steamCmdDirectory = Path.GetDirectoryName(steamCmd);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            ProcessLogOutput processLogOutput = new(ILogOutput.LogType.ExternalProcess);

            // Make sure the installation is up to date
            int updateExitCode = ProcessUtil.Execute(steamCmd, steamCmdDirectory, "+quit", null, processLogOutput.GetAction());
            framework.Environment.UpdateExitCode(updateExitCode);

            if (!CheckoutToken(config))
            {
                throw new Exception("Unable to checkout a token.");
            }

            // Run the upload
            int uploadRetryCount = config.RetryCount;
            int uploadExitCode = -1;
            while (uploadExitCode != 0 && uploadRetryCount > 0)
            {
                uploadExitCode = ProcessUtil.Execute(steamCmd, steamCmdDirectory,
                    $"+login {s_TokenUsername} +run_app_build {config.AppBuild} +quit", null,
                    processLogOutput.GetAction());
                uploadRetryCount--;
                Log.WriteLine("Upload exited with code: " + uploadExitCode);
                if (uploadExitCode == 0)
                {
                    continue;
                }

                Log.WriteLine($"Sleeping 5 seconds before retry ({uploadRetryCount}/{config.RetryCount}) ...");
                Thread.Sleep(5000);
            }
            framework.Environment.UpdateExitCode(uploadExitCode);

            if (!CheckinToken(config))
            {
                Log.WriteLine("There was an issue returning the token.", ILogOutput.LogType.Warning);
            }

        }
        catch (Exception ex)
        {
            s_Token?.Unlock();
            framework.ExceptionHandler(ex);
        }
    }
}