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
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CheckoutManifestItemType
        {
            Git = 0
        }

        [JsonProperty("items")]
        public CheckoutManifestItem[] Items;

        [Serializable]
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


            [JsonProperty("type")]
            public CheckoutManifestItemType Type;

            [JsonProperty("uri")] public string URI;

            public bool Checkout(string basePath)
            {
                var checkoutFolder = System.IO.Path.Combine(basePath, Path);
                Log.WriteLine($"{ID} ({this}) => {checkoutFolder}.", "CHECKOUT");
                switch (Type)
                {
                    case CheckoutManifestItemType.Git:

                        // The output folder does not exist, so we can just do a straight clone
                        if (!System.IO.Directory.Exists(checkoutFolder))
                        {
                           Git.CheckoutRepo(URI, checkoutFolder, Branch);
                        }
                        else
                        {
                            Git.UpdateRepo(checkoutFolder);
                        }
                        break;
                }
                return true;
            }
        }
    }
}