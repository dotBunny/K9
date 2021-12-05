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
        public const string CloneArguments = "clone";
        public const string SwitchBranchArguments = "checkout";
        public const string ResetArguments = "reset --hard";
        public const string UpdateArguments = "pull";

        public const string UpdateOriginArguments = "fetch origin";
        public const string StatusArguments = "status -uno --long";

        public const string AlreadyUpToDateMessage = "Already up-to-date";
        public const string BranchUpToDateMessage = "Your branch is up to date with 'origin/main'";
        public const string PullDryRunArguments = "fetch --dry-run";


        public static void CheckoutRepo(string uri, string checkoutFolder, string branch = null)
        {
            ProcessUtil.ExecuteProcess("git.exe", Directory.GetParent(checkoutFolder).GetPathWithCorrectCase(),
                $"{Git.CloneArguments} {uri} {checkoutFolder}", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });

            if (!string.IsNullOrEmpty(branch))
            {
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    $"{Git.SwitchBranchArguments} {branch}", null, Line =>
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
                $"{Git.UpdateOriginArguments} {checkoutFolder}", null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                });
            ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                Git.StatusArguments, null, Line =>
                {
                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                    output.Add(Line);
                });

            bool branchBehind = false;
            bool fastForward = false;
            foreach (string s in output)
            {
                if (s.Contains("branch is beind"))
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
                    Git.ResetArguments, null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    Git.UpdateArguments, null, Line =>
                    {
                        Log.WriteLine(Line, "GIT");
                    });
            }
            else if (fastForward)
            {
                // We actually need to do something to upgrade this repo
                Log.WriteLine($"Fast-forwarding {checkoutFolder}.", "CHECKOUT");

                ProcessUtil.ExecuteProcess("git.exe", checkoutFolder,
                    Git.UpdateArguments, null, Line =>
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