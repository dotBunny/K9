// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace K9.Unity.PackageManager
{
    public class PackageManifest
    {
        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies = new();

        [NonSerialized]
        private bool _dirty;

        public bool IsDirty()
        {
            return _dirty;
        }

        public bool Has(string id)
        {
            return Dependencies.ContainsKey(id);
        }

        public bool Has(string id, string uri)
        {
            return Dependencies.ContainsKey(id) && Dependencies[id] == uri;
        }

        public void AddOrUpdate(string id, string uri)
        {
            if (Dependencies.ContainsKey(id))
            {
                Dependencies[id] = uri;
                _dirty = true;
            }
            else
            {
                Dependencies.Add(id,uri);
                _dirty = true;
            }
        }
        public bool Remove(string id)
        {
            if (!Dependencies.ContainsKey(id))
            {
                return false;
            }

            Dependencies.Remove(id);
            _dirty = true;
            return true;

        }
    }
}