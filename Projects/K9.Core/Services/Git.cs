// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
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

        public static void CheckoutRepo(string uri, string checkoutFolder, string branch = null)
        {
            ProcessUtil.ExecuteProcess("git.exe", Directory.GetParent(checkoutFolder).GetPathWithCorrectCase(),
                $"clone {uri} {checkoutFolder}", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });

            if (!string.IsNullOrEmpty(branch))
            {
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    $"checkout {branch}", null, Line =>
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
                $"fetch origin {checkoutFolder}", null, Line =>
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