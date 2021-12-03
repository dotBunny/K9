// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

namespace K9.Services
{
    public static class Git
    {
        public const string CloneArguments = "clone";
        public const string SwitchBranchArguments = "checkout";
        public const string ResetArguments = "reset --hard";
        public const string UpdateArguments = "pull";

        /// <summary>
        /// Output what would happen, but dont actually do it
        /// </summary>
        public const string PullDryRunArguments = "fetch --dry-run";

    }
}