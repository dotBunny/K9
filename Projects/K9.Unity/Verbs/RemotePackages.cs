﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using CommandLine;
using K9.Unity.PackageManager;
using Newtonsoft.Json;

namespace K9.Unity.Verbs
{
    [Verb("RemotePackages")]
    public class RemotePackages: IVerb
    {
        [Option('r', "remote", Required = true, HelpText = "The path to a remote package manifest.")]
        public string Remotes { get; set; }

        [Option('u', "unity", Required = true, HelpText = "The Unity project's package manifest.")]
        public string UnityManifest { get; set; }

        private string _cachedUnityManifestContent;
        private PackageManifest _cachedManifest;
        private string _cachedCheckoutManifestContent;
        private CheckoutManifest _cachedCheckoutManifest;

        /// <inheritdoc />
        public bool CanExecute()
        {
            if (!File.Exists(Remotes) || !File.Exists(UnityManifest))
            {
                Log.WriteLine("Unable to find one of the required manifests", Program.Instance.DefaultLogCategory,
                    Log.LogType.Error);
                Core.UpdateExitCode(-1);
                return false;
            }

            return CheckManifests();
        }

        private bool CheckManifests()
        {
            _cachedUnityManifestContent = File.ReadAllText(UnityManifest);
            _cachedManifest = JsonConvert.DeserializeObject<PackageManifest>(_cachedUnityManifestContent);

            if (_cachedManifest == null)
            {
                Log.WriteLine("Unity manifest failed to deserialize.", "PACKAGE", Log.LogType.Error);
                Core.UpdateExitCode(-1);
                return false;
            }

            _cachedCheckoutManifestContent = File.ReadAllText(Remotes);
            _cachedCheckoutManifest = JsonConvert.DeserializeObject<CheckoutManifest>(_cachedCheckoutManifestContent);

            if (_cachedCheckoutManifest == null)
            {
                Log.WriteLine("Remotes manifest failed to deserialize.", "PACKAGE", Log.LogType.Error);
                Core.UpdateExitCode(-1);
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public bool Execute()
        {
            string rootFolder = Path.GetDirectoryName(Remotes);
            string packagesFolder = Path.GetDirectoryName(UnityManifest);

            foreach(CheckoutManifest.CheckoutManifestItem item in _cachedCheckoutManifest.Items)
            {
                // Add to Unity's manifest
                if (!item.HasMapping())
                {
                    string baseFolder = Path.Combine(rootFolder, item.Path);
                    string relativePath = $"file:{Path.GetRelativePath(packagesFolder, baseFolder)}";
                    if(! Directory.Exists(baseFolder))
                    {
                        Log.WriteLine($"Unable to find {baseFolder} as outlined in item {item.ID} => {item.Path}", "PACKAGE", Log.LogType.Error);
                        continue;
                    }

                    if (!_cachedManifest.Is(item.ID, relativePath))
                    {
                        _cachedManifest.AddOrUpdate(item.ID, relativePath);
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, string> mapping in item.Mappings)
                    {
                        string baseFolder = Path.Combine(rootFolder, item.Path, mapping.Value);
                        string relativePath = $"file:{Path.GetRelativePath(packagesFolder, baseFolder)}";
                        if(! Directory.Exists(baseFolder))
                        {
                            Log.WriteLine($"Unable to find {baseFolder} as outlined in mapping {item.ID} => {mapping.Key} => {mapping.Value}", "PACKAGE", Log.LogType.Error);
                            continue;
                        }
                        if (!_cachedManifest.Is(mapping.Key, relativePath) && Directory.Exists(baseFolder))
                        {
                            _cachedManifest.AddOrUpdate(mapping.Key, relativePath);
                        }
                    }
                }
            }

            // Update unity's manifest
            if (_cachedManifest.IsDirty())
            {
                File.WriteAllText(UnityManifest, JsonConvert.SerializeObject(_cachedManifest, Formatting.Indented));
            }

            return true;
        }
    }
}