// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K9.Core;
using K9.Core.Utils;

namespace K9.Services.Git;

public static class GitProvider
{
    static string GetExecutablePath()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT ? "git.exe" : "git";
    }

    public static string? GetRemote(string checkoutFolder)
    {
        ProcessLogCapture logCapture = new();
        ProcessUtil.Execute(GetExecutablePath(), checkoutFolder, "status -sb", null, logCapture.GetAction());
        if (!logCapture.HasContent())
        {
            return null;
        }

        string[] parts = logCapture.GetLastLine().Split(' ')[1].Split("...");
        return parts[1];
    }

    public static string? GetBranch(string checkoutFolder)
    {
        ProcessLogCapture logCapture = new();
        ProcessUtil.Execute(GetExecutablePath(), checkoutFolder, "status -sb", null, logCapture.GetAction());
        if (!logCapture.HasContent())
        {
            return null;
        }

        string[] parts = logCapture.GetLastLine().Split(' ')[1].Split("...");
        return parts[0];
    }

    public static string GetLocalCommit(string checkoutFolder)
    {
        // Check current
        List<string> output = [];

        // Get the status of the repository
        ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
            $"rev-parse HEAD", null, (_, line) =>
            {
                Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
                output.Add(line);
            });

        return output[0].Trim();
    }

    public static string? GetRemoteCommit(string checkoutFolder, string branch = "main")
    {
        // Check current
        List<string> output = [];
        ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
            // ReSharper disable once StringLiteralTypo
            $"ls-remote --sort=committerdate", null, (_, line) =>
            {
                Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
                output.Add(line);
            });

        string branchHead = $"refs/heads/{branch}";
        string? failSafe = null;
        foreach (string? parsed in output.Select(s => s.Trim()))
        {
            if (parsed.EndsWith(branchHead))
            {
                return parsed[..^branchHead.Length].Trim();
            }

            if (parsed.EndsWith("HEAD"))
            {
                failSafe = parsed[..^4].Trim();
            }
        }

        return failSafe;
    }

    public static void CheckoutRepo(string uri, string checkoutFolder, string? branch = null, string? commit = null,
        int depth = -1, bool submodules = true, bool shallowSubmodules = true)
    {
        string executablePath = GetExecutablePath();
        StringBuilder commandLineBuilder = new();
        commandLineBuilder.Append("clone ");

        // Was a branch defined?
        if (branch != null)
        {
            commandLineBuilder.AppendFormat("--branch {0} --single-branch ", branch);
        }

        // Do we have a specific depth to go too?
        if (depth != -1)
        {
            commandLineBuilder.AppendFormat("--depth {0} ", depth.ToString());
        }

        if (submodules)
        {
            commandLineBuilder.Append("--recurse-submodules --remote-submodules ");
            if (shallowSubmodules)
            {
                commandLineBuilder.Append("--shallow-submodules ");
            }
        }

        ProcessLogRedirect logRedirect = new(ILogOutput.LogType.ExternalProcess, "GIT");

        Log.WriteLine($"{commandLineBuilder}{uri} {checkoutFolder}", "GIT");
        DirectoryInfo? info = Directory.GetParent(checkoutFolder);
        if (info != null)
        {
            ProcessUtil.Execute(executablePath,info.GetPathWithCorrectCase(),
                $"{commandLineBuilder}{uri} {checkoutFolder}", null, logRedirect.GetAction());
        }
        else
        {
            Log.WriteLine($"Unable to find parent directory for {checkoutFolder}", ILogOutput.LogType.Error, "GIT");
            return;
        }

        // Was a commit specified?
        if (commit == null)
        {
            return;
        }

        Log.WriteLine($"Checkout Commit {commit}", "GIT", ILogOutput.LogType.ExternalProcess);
        ProcessUtil.Execute(executablePath, checkoutFolder,
            $"checkout {commit}", null, logRedirect.GetAction());
    }

    public static void InitializeSubmodules(string checkoutFolder) //, int depth = -1)
    {
        StringBuilder commandLineBuilder = new();
        commandLineBuilder.Append("submodule init ");

        // if (depth != -1)
        // {
        //     commandLineBuilder.AppendFormat("--depth {0} ", depth.ToString());
        // }
        // if (depth == 1)
        // {
        //     commandLineBuilder.Append("--shallow-submodules ");
        // }

        Log.WriteLine("Initialize Submodules", "GIT", ILogOutput.LogType.ExternalProcess);
        ProcessLogRedirect logRedirect = new(ILogOutput.LogType.ExternalProcess, "GIT");
        ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
            commandLineBuilder.ToString(), null, logRedirect.GetAction());
    }

    public static void UpdateSubmodule(string checkoutFolder, int depth = -1, string? submodule = null)
    {
        StringBuilder commandLineBuilder = new();
        commandLineBuilder.Append("submodule update ");
        if (depth == 1)
        {
            commandLineBuilder.Append("--recommend-shallow ");
        }

        commandLineBuilder.Append("--remote ");

        if (!string.IsNullOrEmpty(submodule))
        {
            commandLineBuilder.Append(submodule);
        }

        Log.WriteLine(submodule != null ? $"Update Submodule [{submodule}]" : $"Update Submodule", "GIT",
            ILogOutput.LogType.ExternalProcess);

        ProcessLogRedirect logRedirect = new(ILogOutput.LogType.ExternalProcess, "GIT");
        ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
            commandLineBuilder.ToString().Trim(), null, logRedirect.GetAction());
    }

    public static void UpdateRepo(string checkoutFolder, string? branch = null, string? commit = null,
        bool forceUpdate = true)
    {
        string executablePath = GetExecutablePath();
        // Check current
        List<string> output = [];
        ProcessLogRedirect logRedirect = new(ILogOutput.LogType.ExternalProcess, "GIT");

        // Get the status of the repository
        ProcessUtil.Execute(executablePath, checkoutFolder, "fetch origin", null, logRedirect.GetAction());
        ProcessUtil.Execute(executablePath, checkoutFolder,
            "status -uno --long", null, (_, line) =>
            {
                Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
                output.Add(line);
            });

        bool branchBehind = false;
        bool fastForward = false;
        bool detached = false;
        foreach (string s in output)
        {
            if (s.Contains("branch is behind"))
            {
                branchBehind = true;
            }

            if (s.Contains("and can be fast-forwarded"))
            {
                fastForward = true;
            }

            if (s.Contains("HEAD detached at"))
            {
                detached = true;
            }
        }

        if (detached)
        {
            ProcessUtil.Execute(executablePath, checkoutFolder, "reset --hard", null, logRedirect.GetAction());

            if (branch != null)
            {
                ProcessUtil.Execute(executablePath, checkoutFolder, $"switch {branch}", null, logRedirect.GetAction());
            }

            ProcessUtil.Execute(executablePath, checkoutFolder, "pull", null, logRedirect.GetAction());

            if (commit != null)
            {
                ProcessUtil.Execute(executablePath, checkoutFolder, $"checkout {commit}", null,
                    logRedirect.GetAction());
                Log.WriteLine($"{checkoutFolder} detached updated to {commit}.", "CHECKOUT");
            }
            else
            {
                Log.WriteLine($"{checkoutFolder} detached head reset to latest.", "CHECKOUT");
            }
        }
        else if ((!fastForward && branchBehind) || forceUpdate)
        {
            // We actually need to do something to upgrade this repository
            Log.WriteLine($"{checkoutFolder} needs updating, resetting as it could not be cleanly updated.",
                "CHECKOUT");

            ProcessUtil.Execute(executablePath, checkoutFolder, "reset --hard", null, logRedirect.GetAction());
            ProcessUtil.Execute(executablePath, checkoutFolder, "pull", null, logRedirect.GetAction());
        }
        else if (fastForward)
        {
            // We actually need to do something to upgrade this repository
            Log.WriteLine($"Fast-forwarding {checkoutFolder}.", "CHECKOUT");

            ProcessUtil.Execute(executablePath, checkoutFolder, "pull", null, logRedirect.GetAction());
        }
        else
        {
            Log.WriteLine($"{checkoutFolder} is up-to-date.", "CHECKOUT");
        }

        // Clear our cached output
        output.Clear();
    }
}