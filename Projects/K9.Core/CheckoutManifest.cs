// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using K9.Services;
using K9.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
                Log.WriteLine($"Processing {ID} ({Type}) => {outputPath}.");

                // Check current
                List<string> output = new ();

                switch (Type)
                {
                    case CheckoutManifestItemType.Git:

                        // Check output
                        if (!System.IO.Directory.Exists(outputPath))
                        {
                            // TOOD: Feel like we probably want to show whats going on?
                            ProcessUtil.ExecuteProcess("git.exe", basePath,
                                $"{Git.CloneArguments} {URI} {outputPath}", null, out output);
                            break;
                        }


                        break;
                }

                return true;
            }
        }
    }
}