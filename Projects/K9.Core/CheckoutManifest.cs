// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using K9.Services;
using K9.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Utilities;

namespace K9
{
    [Serializable]
    public class CheckoutManifest
    {
        public enum CheckoutManifestItemType
        {
            Git = 0
        }

        [JsonProperty("items")] public CheckoutManifestItem[] Items;

        public class CheckoutManifestItem
        {
            /// <inheritdoc />
            public override string ToString()
            {
                return $"{Type.ToString()}://{URI}/{Branch}#{Commit}";
            }

            [JsonProperty("branch")] public string Branch;
            [JsonProperty("commit")] public string Commit;
            [JsonProperty("id")] public string ID;
            [JsonProperty("path")] public string Path;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("type")]
            public CheckoutManifestItemType Type;

            [JsonProperty("uri")] public string URI;

            public bool Checkout(string basePath)
            {
                var outputPath = System.IO.Path.Combine(basePath, Path);
                Log.WriteLine($"{ID} ({this}) => {outputPath}.", "CHECKOUT");

                // Check current
                List<string> output = new ();

                switch (Type)
                {
                    case CheckoutManifestItemType.Git:

                        // The output folder does not exist, so we can just do a straight clone
                        if (!System.IO.Directory.Exists(outputPath))
                        {
                            ProcessUtil.ExecuteProcess("git.exe", basePath,
                                $"{Git.CloneArguments} {URI} {outputPath}", null, Line =>
                                {
                                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                                });
                            ProcessUtil.ExecuteProcess("git.exe", outputPath,
                                $"{Git.SwitchBranchArguments} {Branch}", null, Line =>
                                {
                                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                                });
                        }
                        else
                        {
                            // Get status of the repository
                            ProcessUtil.ExecuteProcess("git.exe", outputPath,
                                Git.PullDryRunArguments, null, Line =>
                                {
                                    Log.WriteLine(Line, "GIT", Log.LogType.ExternalProcess);
                                    output.Add(Line);
                                });

                            bool foundAnything = false;
                            foreach (string s in output)
                            {
                                if (!string.IsNullOrEmpty(s))
                                {
                                    foundAnything = true;
                                    break;
                                }
                            }

                            // Clear our cached output
                            output.Clear();

                            if (!foundAnything)
                            {
                                Log.WriteLine($"{ID} is up-to-date.", "CHECKOUT");
                            }
                            else
                            {
                                // We actually need to do something to upgrade this repo
                                Log.WriteLine($"{ID} needs updating.", "CHECKOUT");
                                ProcessUtil.ExecuteProcess("git.exe", outputPath,
                                    Git.ResetArguments, null, Line =>
                                    {
                                        Log.WriteLine(Line, "GIT");
                                    });
                                ProcessUtil.ExecuteProcess("git.exe", outputPath,
                                    Git.UpdateArguments, null, Line =>
                                    {
                                        Log.WriteLine(Line, "GIT");
                                    });
                            }
                        }
                        break;
                }
                return true;
            }
        }
    }
}