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
    public class CheckoutManifest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CheckoutManifestItemType
        {
            Git = 0
        }

        [JsonProperty("items")]
        public CheckoutManifestItem[] Items;

        public class CheckoutManifestItem
        {
            /// <inheritdoc />
            public override string ToString()
            {
                return $"{Type.ToString()}://{URI}/{Branch}#{Commit}";
            }

            [JsonProperty("branch")]
            public string Branch;

            [JsonProperty("commit")]
            public string Commit; // TODO: Support at commit checkouts

            [JsonProperty("id")]
            public string ID;

            [JsonProperty("path")]
            public string Path;

            [JsonProperty("type")]
            public CheckoutManifestItemType Type;

            [JsonProperty("uri")]
            public string URI;

            [JsonProperty("sparse")]
            public string[] SparseDefinitions;

            [JsonProperty("submodules")]
            public string[] Submodules;

            [JsonProperty("mappings")]
            public Dictionary<string, string> Mappings = new();

            public bool HasMapping()
            {
                return Mappings.Count > 0;
            }
            public bool Checkout(string basePath, int depth = 1)
            {
                var checkoutFolder = System.IO.Path.Combine(basePath, Path);
                Log.WriteLine($"{ID} ({this}) => {checkoutFolder}.", "CHECKOUT");
                switch (Type)
                {
                    case CheckoutManifestItemType.Git:

                        // The output folder does not exist, so we can just do a straight clone
                        if (!System.IO.Directory.Exists(checkoutFolder))
                        {
                            Git.CheckoutRepo(URI, checkoutFolder, Branch, Commit, depth, false);
                            if (Submodules != null && Submodules.Length > 0)
                            {
                                Git.InitializeSubmodules(checkoutFolder, depth);
                                foreach (string s in Submodules)
                                {
                                    Git.UpdateSubmodule(
                                        checkoutFolder,
                                        depth,
                                        s);
                                }
                            }

                            break;
                        }

                        string localCommit = Git.GetLocalCommit(checkoutFolder);
                        if (string.IsNullOrEmpty(Commit))
                        {
                            Git.UpdateRepo(checkoutFolder, Branch, null, false);
                            if (Submodules != null && Submodules.Length > 0)
                            {
                                foreach (string s in Submodules)
                                {
                                    Git.UpdateSubmodule(
                                        checkoutFolder,
                                        depth,
                                        s);
                                }
                            }

                            break;
                        }
                        else if (!string.IsNullOrEmpty(Commit) && Commit != localCommit)
                        {
                            Git.UpdateRepo(checkoutFolder, Branch, Commit, false);
                            if (Submodules != null && Submodules.Length > 0)
                            {
                                foreach (string s in Submodules)
                                {
                                    Git.UpdateSubmodule(
                                        System.IO.Path.Combine(checkoutFolder, s),
                                        depth,
                                        s);
                                }
                            }

                            break;
                        }
                        Log.WriteLine($"Nothing to do.", "CHECKOUT");
                        break;
                }
                return true;
            }
        }
    }
}