// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;
using Newtonsoft.Json;

namespace K9.Setup.Verbs
{
    [Verb("Checkout")]
    public class Checkout : IVerb
    {
        [Option('m', "manifest", Required = true, HelpText = "The manifest to use for checking out different repositories.")]
        public string Manifest { get; set; }

        private CheckoutManifest _cachedManifest;
        private string _cachedManifestContents;

        /// <inheritdoc />
        public bool CanExecute()
        {
            if (string.IsNullOrEmpty(Manifest))
            {
                Log.WriteLine("No manifest provided.", "CHECKOUT", Log.LogType.Error);
                return false;
            }

            if (!System.IO.File.Exists(Manifest))
            {
                Log.WriteLine("Unable to find manifest.", "CHECKOUT", Log.LogType.Error);
                return false;
            }

            if (CheckManifest())
            {
                return true;
            }

            Log.WriteLine("Manifest failed to deserialize.", "CHECKOUT", Log.LogType.Error);
            return false;
        }

        /// <inheritdoc />
        public bool Execute()
        {
            if (!CheckManifest())
            {
                return false;
            }

            string basePath = System.IO.Path.GetDirectoryName(Manifest);
            int positiveCount = 0;
            foreach (CheckoutManifest.CheckoutManifestItem item in _cachedManifest.Items)
            {
                if (item.Checkout(basePath))
                {
                    positiveCount++;
                }
            }
            return positiveCount == _cachedManifest.Items.Length;
        }

        private bool CheckManifest()
        {
            _cachedManifestContents ??= System.IO.File.ReadAllText(Manifest);
            _cachedManifest ??= JsonConvert.DeserializeObject<CheckoutManifest>(_cachedManifestContents);

            return _cachedManifest != null;
        }
    }
}