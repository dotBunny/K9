// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using K9.Utils;

namespace K9.Services
{
    public static class Git
    {
        public static string GetExecutablePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "git.exe";
            }
            else
            {
                return "git";
            }
        }

        public static string GetRemote(string checkoutFolder)
        {
            string output = null;
            ProcessUtil.ExecuteProcess(GetExecutablePath(), checkoutFolder,
                "status -sb", null, Line =>
                {
                    if (!string.IsNullOrEmpty(Line))
                    {
                        output = Line;
                    }
                });

            if (!string.IsNullOrEmpty(output))
            {
                string[] parts = output.Split(' ')[1].Split("...");
                return parts[1];
            }
            return null;
        }
        public static string GetBranch(string checkoutFolder)
        {
            string output = null;
            ProcessUtil.ExecuteProcess(GetExecutablePath(), checkoutFolder,
                "status -sb", null, Line =>
                {
                    if (!string.IsNullOrEmpty(Line))
                    {
                        output = Line;
                    }
                });

            if (!string.IsNullOrEmpty(output))
            {
                string[] parts = output.Split(' ')[1].Split("...");
                return parts[0];
            }
            return null;
        }

        public static string GetLocalCommit(string checkoutFolder)
        {
            // Check current
            List<string> output = new ();

            // Get status of the repository
            ProcessUtil.ExecuteProcess(GetExecutablePath(), checkoutFolder,
                $"rev-parse HEAD", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    output.Add(Line);
                });

            return output[0].Trim();
        }

        public static void CheckoutRepo(string uri, string checkoutFolder, string branch = null, string commit = null, int depth = -1, bool submodules = true, bool shallowsubmodules = true)
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
                if (shallowsubmodules)
                {
                    commandLineBuilder.Append("--shallow-submodules ");
                }
            }

            Log.WriteLine($"{commandLineBuilder}{uri} {checkoutFolder}", "GIT");
            ProcessUtil.ExecuteProcess(executablePath, Directory.GetParent(checkoutFolder).GetPathWithCorrectCase(),
                $"{commandLineBuilder}{uri} {checkoutFolder}", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });

            // Was a commit specified?
            if (commit != null)
            {
                Log.WriteLine($"Checkout Commit {commit}", "GIT", Log.LogType.ExternalProcess);
                ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                    $"checkout {commit}", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    });
            }
        }

        public static void InitializeSubmodules(string checkoutFolder, int depth = -1)
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

            Log.WriteLine("Initialize Submodules", "GIT", Log.LogType.ExternalProcess);
            ProcessUtil.ExecuteProcess(GetExecutablePath(), checkoutFolder,
                commandLineBuilder.ToString(), null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });
        }
        public static void UpdateSubmodule(string checkoutFolder, int depth = -1, string  submodule = null)
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
                Log.LogType.ExternalProcess);

            ProcessUtil.ExecuteProcess(GetExecutablePath(), checkoutFolder,
                commandLineBuilder.ToString().Trim(), null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });
        }

        public static void UpdateRepo(string checkoutFolder, string branch = null, string commit = null, bool forceUpdate = true)
        {
            string executablePath = GetExecutablePath();
            // Check current
            List<string> output = new ();

            // Get status of the repository
            ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                $"fetch origin", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });
            ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                "status -uno --long", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    output.Add(Line);
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
                ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                    $"reset --hard", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });

                if (branch != null)
                {
                    ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                        $"switch {branch}", null, Line =>
                        {
                            Log.WriteLine(Line, "GIT");
                        });
                }

                ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                    "pull", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });

                if (commit != null)
                {
                    ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                        $"checkout {commit}", null, Line =>
                        {
                            Log.WriteLine(Line, "GIT");
                        });
                    Log.WriteLine($"{checkoutFolder} detached updated to {commit}.", "CHECKOUT");
                }
                else
                {
                    Log.WriteLine($"{checkoutFolder} detached head reset to latest.", "CHECKOUT");
                }
            }
            else if ((!fastForward && branchBehind) || forceUpdate)
            {
                // We actually need to do something to upgrade this repo
                Log.WriteLine($"{checkoutFolder} needs updating, resetting as it could not be cleanly updated.", "CHECKOUT");

                ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                    "reset --hard", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
                ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                    "pull", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
            }
            else if (fastForward)
            {
                // We actually need to do something to upgrade this repo
                Log.WriteLine($"Fast-forwarding {checkoutFolder}.", "CHECKOUT");

                ProcessUtil.ExecuteProcess(executablePath, checkoutFolder,
                    "pull", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
            }
            else
            {
                Log.WriteLine($"{checkoutFolder} is up-to-date.", "CHECKOUT");
            }

            // Clear our cached output
            output.Clear();
        }

    }
}