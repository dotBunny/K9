// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;

namespace K9.Unity
{
    public class DefaultOptions
    {
        [Option('f', "folder", Required = false, HelpText = "Target Folder")]
        public string Folder { get; set; }
    }
}