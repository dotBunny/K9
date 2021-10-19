// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;

namespace K9.Unity.Verbs
{
    public class Packages : IVerb
    {
        [Option('m', "manifest", Required = false, HelpText = "The checkout manifest to add to the Unity project's manifest..")]
        public string Manifest { get; set; }


        /// <inheritdoc />
        public bool CanExecute()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public bool Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}