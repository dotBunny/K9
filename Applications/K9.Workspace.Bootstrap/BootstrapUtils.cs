// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.Build.Locator;

namespace K9.Workspace.Bootstrap;

public static class BootstrapUtils
{
    class ProcessLogObject(Action<int, string> outputAction)
    {
        private int _processIdentifier;

        private readonly object _lockObject = new();

        public void SetProcessIdentifier(int processIdentifier)
        {
            lock (_lockObject)
            {
                _processIdentifier = processIdentifier;
            }
        }
        public void OutputHandler(object x, DataReceivedEventArgs y)
        {
            if (y.Data == null)
            {
                return;
            }

            lock (_lockObject)
            {
                outputAction(_processIdentifier, y.Data.TrimEnd());
            }
        }
    }

    const int k_AsciiCaseShift = 32;
    const int k_AsciiLowerCaseStart = 97;
    const int k_AsciiLowerCaseEnd = 122;

    [SecuritySafeCritical]
    public static unsafe int GetStableUpperCaseHashCode(this string targetString)
    {
        fixed (char* src = targetString)
        {
            int hash1 = 5381;
            int hash2 = hash1;
            int c;
            char* s = src;

            // Get character
            while ((c = s[0]) != 0)
            {
                // Check character value and shift it if necessary (32)
                if (c is >= k_AsciiLowerCaseStart and <= k_AsciiLowerCaseEnd)
                {
                    c ^= k_AsciiCaseShift;
                }

                // Add to Hash #1
                hash1 = ((hash1 << 5) + hash1) ^ c;

                // Get our second character
                c = s[1];

                if (c == 0)
                {
                    break;
                }

                // Check character value and shift it if necessary (32)
                if (c is >= k_AsciiLowerCaseStart and <= k_AsciiLowerCaseEnd)
                {
                    c ^= k_AsciiCaseShift;
                }

                hash2 = ((hash2 << 5) + hash2) ^ c;
                s += 2;
            }

            return hash1 + hash2 * 1566083941;
        }
    }

    // ReSharper disable once InconsistentNaming
    public static string? GetMSBuild()
    {
        // Check using the Visual Studio instance finder first
        IEnumerable<VisualStudioInstance> quickFind = MSBuildLocator.QueryVisualStudioInstances();
        IEnumerable<VisualStudioInstance> visualStudioInstances = quickFind as VisualStudioInstance[] ?? quickFind.ToArray();
        if (quickFind != null && visualStudioInstances.Any())
        {
            return visualStudioInstances.OrderByDescending(instance => instance.Version).First().MSBuildPath;
        }

        // Do we have the SDK even installed?
        if (!Directory.Exists(@"C:\Program Files\dotnet\sdk"))
        {
            return null;
        }

        // Find any instance of MSBuild.dll
        List<string> foundPaths = [];
        IEnumerable<string> folders = Directory.EnumerateDirectories(@"C:\Program Files\dotnet\sdk", "*", SearchOption.TopDirectoryOnly);
        foundPaths.AddRange(folders.Select(folder => Path.Combine(folder, "MSBuild.dll")).Where(File.Exists));
        if (foundPaths.Count <= 0)
        {
            return null;
        }
        foundPaths.Sort();
        return foundPaths[^1];
    }

    public static int ProcessExecute(string executablePath, string? workingDirectory, string? arguments, string? input, Action<int, string> outputLine)
    {
        using Process childProcess = new();
        ProcessLogObject logObject = new(outputLine);

        childProcess.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true";
        if (workingDirectory != null)
        {
            childProcess.StartInfo.WorkingDirectory = workingDirectory;
        }
        childProcess.StartInfo.FileName = executablePath;
        childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
        childProcess.StartInfo.UseShellExecute = false;
        childProcess.StartInfo.RedirectStandardOutput = true;
        childProcess.StartInfo.RedirectStandardError = true;
        childProcess.OutputDataReceived += logObject.OutputHandler;
        childProcess.ErrorDataReceived += logObject.OutputHandler;
        childProcess.StartInfo.RedirectStandardInput = input != null;
        childProcess.StartInfo.CreateNoWindow = true;
        childProcess.StartInfo.StandardOutputEncoding = new UTF8Encoding(false, false);
        childProcess.Start();
        logObject.SetProcessIdentifier(childProcess.Id);
        childProcess.BeginOutputReadLine();
        childProcess.BeginErrorReadLine();

        if (!string.IsNullOrEmpty(input))
        {
            childProcess.StandardInput.WriteLine(input);
            childProcess.StandardInput.Close();
        }

        // Busy wait for the process to exit so we can get a ThreadAbortException if the thread is terminated.
        // It won't wait until we enter managed code again before it throws otherwise.
        for (; ; )
        {
            if (!childProcess.WaitForExit(20))
            {
                continue;
            }

            childProcess.WaitForExit();
            break;
        }

        return childProcess.ExitCode;
    }

    public static void ProcessElevate(string executablePath, string? workingDirectory, string? arguments)
    {
        Process childProcess = new();
        if (workingDirectory != null)
        {
            childProcess.StartInfo.WorkingDirectory = workingDirectory;
        }
        childProcess.StartInfo.FileName = executablePath;
        childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
        childProcess.StartInfo.UseShellExecute = true;
        childProcess.StartInfo.Verb = "runas";
        childProcess.StartInfo.CreateNoWindow = true;
        childProcess.Start();

        // Busy wait for the process to exit so we can get a ThreadAbortException if the thread is terminated.
        // It won't wait until we enter managed code again before it throws otherwise.
        for (; ; )
        {
            if (!childProcess.WaitForExit(20))
            {
                continue;
            }

            childProcess.WaitForExit();
            break;
        }
    }

    public static void GitCheckoutRepo(string uri, string checkoutFolder, string? branch = null, string? commit = null, int depth = -1, bool submodules = true, bool shallowsubmodules = true)
    {
        string executablePath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "git.exe" : "git";
        StringBuilder commandLineBuilder = new();
        commandLineBuilder.Append("clone ");

        if (branch != null)
        {
            commandLineBuilder.Append($"--branch {branch} --single-branch ");
        }

        if (depth != -1)
        {
            commandLineBuilder.Append($"--depth {depth.ToString()} ");
        }

        if (submodules)
        {
            commandLineBuilder.Append("--recurse-submodules --remote-submodules ");
            if (shallowsubmodules)
            {
                commandLineBuilder.Append("--shallow-submodules ");
            }
        }

        Console.WriteLine($"{commandLineBuilder}{uri} {checkoutFolder}");
        ProcessExecute(executablePath, null,
            $"{commandLineBuilder}{uri} {checkoutFolder}", null, (processIdentifier, line) =>
            {
                Console.WriteLine($"[{processIdentifier}] {line}");
            });

        // Was a commit specified?
        if (commit != null)
        {
            Console.WriteLine($"Checkout Commit {commit}");
            ProcessExecute(executablePath, checkoutFolder,
                $"checkout {commit}", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}] {line}");
                });
        }
    }
    public static string GitGetLocalCommit(string checkoutFolder)
    {
        string executablePath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "git.exe" : "git";

        // Check current
        List<string> output = [];

        // Get status of the repository
        ProcessExecute(executablePath, checkoutFolder,
            $"rev-parse HEAD", null, (processIdentifier, line) =>
            {
                Console.WriteLine($"[{processIdentifier}] {line}");
                output.Add(line);
            });

        return output[0].Trim();
    }

    public static void GitUpdateRepo(string checkoutFolder, string? branch = null, string? commit = null, bool forceUpdate = true)
    {
        string executablePath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "git.exe" : "git";

        // Check current
        List<string> output = [];

        // Get status of the repository
        ProcessExecute(executablePath, checkoutFolder,
            $"fetch origin", null, (processIdentifier, line) =>
            {
                Console.WriteLine($"[{processIdentifier}] {line}");
            });
        ProcessExecute(executablePath, checkoutFolder,
            "status -uno --long", null, (processIdentifier, line) =>
            {
                Console.WriteLine($"[{processIdentifier}] {line}");
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
            ProcessExecute(executablePath, checkoutFolder,
                $"reset --hard", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}] {line}");
                });

            if (branch != null)
            {
                ProcessExecute(executablePath, checkoutFolder,
                    $"switch {branch}", null, (processIdentifier, line) =>
                    {
                        Console.WriteLine($"[{processIdentifier}] {line}");
                    });
            }

            ProcessExecute(executablePath, checkoutFolder,
                "pull", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}] {line}");
                });

            if (commit != null)
            {
                ProcessExecute(executablePath, checkoutFolder,
                    $"checkout {commit}", null, (processIdentifier, line) =>
                    {
                        Console.WriteLine($"[{processIdentifier}] {line}");
                    });
                Console.WriteLine($"{checkoutFolder} detached updated to {commit}.");
            }
            else
            {
                Console.WriteLine($"{checkoutFolder} detached head reset to latest.");
            }
        }
        else if ((!fastForward && branchBehind) || forceUpdate)
        {
            // We actually need to do something to upgrade this repository
            Console.WriteLine($"{checkoutFolder} needs updating, resetting as it could not be cleanly updated.");

            ProcessExecute(executablePath, checkoutFolder,
                "reset --hard", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}] {line}");
                });
            ProcessExecute(executablePath, checkoutFolder,
                "pull", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}] {line}");
                });
        }
        else if (fastForward)
        {
            // We actually need to do something to upgrade this repository
            Console.WriteLine($"Fast-forwarding {checkoutFolder}.");

            ProcessExecute(executablePath, checkoutFolder,
                "pull", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}] {line}");
                });
        }
        else
        {
            Console.WriteLine($"{checkoutFolder} is up-to-date.");
        }

        // Clear our cached output
        output.Clear();
    }


}