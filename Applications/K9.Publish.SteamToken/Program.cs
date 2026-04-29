// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Threading;
using K9.Core;
using K9.Core.IO;
using K9.Core.Utils;

namespace K9.Publish.SteamToken;

internal static class Program
{
    static FileLock? s_Token;
    static string? s_TokenUsername;

    static bool CheckoutToken(SteamTokenProvider provider)
    {
        // Get token list / lock
        string[] foundTokens = Directory.GetFiles(provider.TokenFolder, "*.vdf");
        if (foundTokens.Length <= 0)
        {
            throw new Exception($"Unable to find tokens (*.vdf) at {provider.TokenFolder}.");
        }
        Log.WriteLine($"Found {foundTokens.Length} Tokens In Pool.");

        // Handle Specific Target
        if (provider.Token != null)
        {
            s_Token = new FileLock(Path.Combine(provider.TokenFolder, provider.Token + ".vdf"));
            s_Token.Lock(provider.ForceFlag);
            if (!s_Token.HasLock())
            {

                if (!s_Token.SafeLock())
                {
                    throw new Exception($"Was unable to acquire lock to {provider.Token}");
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


        if (s_Token == null || provider.TokenTarget == null)
        {
            return false;
        }

        // We need to ensure the folder we are writing exists as it might be a brand-new installation

        FileUtil.EnsureFileFolderHierarchyExists(provider.TokenTarget);
        File.Copy(s_Token.FilePath, provider.TokenTarget, true);

        s_TokenUsername = Path.GetFileNameWithoutExtension(s_Token.FilePath);

        Log.WriteLine($"Checked out {s_Token.FilePath} to {provider.TokenTarget} for user {s_TokenUsername}.");

        return true;
    }
    static bool CheckinToken(SteamTokenProvider provider)
    {

        if (s_Token == null)
        {
            throw new Exception("Attempting to check-in a token that is null.");
        }
        if (s_Token.HasLock())
        {
            if (provider.TokenTarget != null)
            {
                Log.WriteLine($"Returned {provider.TokenTarget} to {s_Token.FilePath}.");

                // Copy File
                File.Copy(provider.TokenTarget, s_Token.FilePath, true);
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
            DefaultLogCategory = "STEAMTOKEN",
            LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
        }, new SteamTokenProvider());

        try
        {
            SteamTokenProvider provider = (SteamTokenProvider)framework.ProgramProvider;
            provider.EnsureNetworkPath();

            // Check for existing installation
            if (!Directory.Exists(Path.Combine(provider.InstallLocation, "sdk")))
            {
                // We don't have an existing version installation we need to grab the package
                Log.WriteLine($"Installing Steamworks SDK to {provider.InstallLocation}.");
                System.IO.Compression.ZipFile.ExtractToDirectory(provider.InstallPackage, provider.InstallLocation);
            }

            // ReSharper disable StringLiteralTypo
            string steamCmd = Path.Combine(provider.InstallLocation, "sdk", "tools", "ContentBuilder", "builder", "steamcmd.exe");
            // ReSharper restore StringLiteralTypo


            if (!File.Exists(steamCmd))
            {
                throw new Exception("Unable to find SteamCMD");
            }

            string? steamCmdDirectory = Path.GetDirectoryName(steamCmd);

            // TODO: if we moved this to log replacer and had it handle errors HORDE wouldnt error unless acutal problem

            ProcessLogRedirect processLogRedirect = new(ILogOutput.LogType.ExternalProcess);

            // Make sure the installation is up to date
            int updateExitCode = ProcessUtil.Execute(steamCmd, steamCmdDirectory, "+quit", null, processLogRedirect.GetAction());
            framework.Environment.UpdateExitCode(updateExitCode);

            if (!CheckoutToken(provider))
            {
                throw new Exception("Unable to checkout a token.");
            }

            // Run the upload
            int uploadRetryCount = provider.RetryCount;
            int uploadExitCode = -1;
            while (uploadExitCode != 0 && uploadRetryCount > 0)
            {
                uploadExitCode = ProcessUtil.Execute(steamCmd, steamCmdDirectory,
                    $"+login {s_TokenUsername} +run_app_build {provider.AppBuild} +quit", null,
                    processLogRedirect.GetAction());
                uploadRetryCount--;
                Log.WriteLine("Upload exited with code: " + uploadExitCode);
                if (uploadExitCode == 0)
                {
                    continue;
                }

                Log.WriteLine($"Sleeping 5 seconds before retry ({uploadRetryCount}/{provider.RetryCount}) ...");
                Thread.Sleep(5000);
            }
            framework.Environment.UpdateExitCode(uploadExitCode);

            if (!CheckinToken(provider))
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