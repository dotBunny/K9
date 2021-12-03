// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;
using K9.Unity.PackageManager;
using Newtonsoft.Json;

namespace K9.Unity.Verbs
{
    [Verb("RemovePackage")]
    public class RemovePackage : IVerb
    {
        [Option('m', "manifest", Required = true, HelpText = "The Unity project's package manifest.")]
        public string Manifest { get; set; }

        [Option('i', "id", Required = true, HelpText = "The package id to remove.")]
        public string ID { get; set;  }

        private PackageManifest _cachedManifest;
        private string _cachedManifestContents;

        /// <inheritdoc />
        public bool CanExecute()
        {
            if (string.IsNullOrEmpty(Manifest))
            {
                Log.WriteLine("No manifest provided.", "PACKAGE", Log.LogType.Error);
                return false;
            }

            if (!System.IO.File.Exists(Manifest))
            {
                Log.WriteLine("Unable to find manifest.", "PACKAGE", Log.LogType.Error);
                return false;
            }

            if (CheckManifest())
            {
                return true;
            }

            Log.WriteLine("Manifest failed to deserialize.", "PACKAGE", Log.LogType.Error);
            return false;
        }

        /// <inheritdoc />
        public bool Execute()
        {
            if (_cachedManifest.Remove(ID))
            {
                System.IO.File.WriteAllText(Manifest, JsonConvert.SerializeObject(_cachedManifest, Formatting.Indented));
            }
            return true;
        }

        private bool CheckManifest()
        {
            _cachedManifestContents ??= System.IO.File.ReadAllText(Manifest);
            _cachedManifest ??= JsonConvert.DeserializeObject<PackageManifest>(_cachedManifestContents);

            return _cachedManifest != null;
        }
    }
}