// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using K9.Utils;

namespace K9.Services
{
    public static class Git
    {
        public static string GetRemote(string checkoutFolder)
        {
            string output = null;
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
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
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
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
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                $"rev-parse HEAD", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    output.Add(Line);
                });

            return output[0].Trim();
        }

        public static void CheckoutRepo(string uri, string checkoutFolder, string branch = null, string commit = null, int depth = -1, bool submodules = true, bool shallowsubmodules = true)
        {
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
            ProcessUtil.ExecuteProcess("git.exe", Directory.GetParent(checkoutFolder).GetPathWithCorrectCase(),
                $"{commandLineBuilder}{uri} {checkoutFolder}", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });

            // Was a commit specified?
            if (commit != null)
            {
                Log.WriteLine($"Checkout Commit {commit}", "GIT", Log.LogType.ExternalProcess);
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    $"checkout {commit}", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    });
            }
        }

        public static void InitializeSubmodules(string checkoutFolder)
        {
            Log.WriteLine("Initialize Submodules", "GIT", Log.LogType.ExternalProcess);
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                $"submodule init", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });
        }
        public static void UpdateSubmodule(string checkoutFolder, string  submodule = null)
        {
            if (submodule != null)
            {
                Log.WriteLine($"Update Submodule [{submodule}]", "GIT", Log.LogType.ExternalProcess);
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    $"submodule update --remote {submodule}", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    });
            }
            else
            {
                Log.WriteLine($"Update Submodules", "GIT", Log.LogType.ExternalProcess);
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    $"submodule update", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    });
            }
        }

        public static void UpdateRepo(string checkoutFolder, bool forceUpdate = true)
        {
            // Check current
            List<string> output = new ();

            // Get status of the repository
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                $"fetch origin", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                "status -uno --long", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    output.Add(Line);
                });

            bool branchBehind = false;
            bool fastForward = false;
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
            }

            if ((!fastForward && branchBehind) || forceUpdate)
            {
                // We actually need to do something to upgrade this repo
                Log.WriteLine($"{checkoutFolder} needs updating, resetting as it could not be cleanly updated.", "CHECKOUT");

                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    "reset --hard", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    "pull", null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
            }
            else if (fastForward)
            {
                // We actually need to do something to upgrade this repo
                Log.WriteLine($"Fast-forwarding {checkoutFolder}.", "CHECKOUT");

                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
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