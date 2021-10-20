// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace K9.Unity.PackageManager
{
    public class PackageManifest
    {
        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies = new();
    }
}