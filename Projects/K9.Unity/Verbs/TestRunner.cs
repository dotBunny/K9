// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;

namespace K9.Unity.Verbs
{
    [Verb("TestRunner")]
    public class TestRunner : IVerb
    {
        [Option('r', "run", Required = false, HelpText = "")]
        public string Run { get; set; }

        [Option('t', "testPlatform", Required = false, Default = "editmode", HelpText = "")]
        public string Mode { get; set; }

        [Option('c', "category", Required = false, HelpText = "")]
        public string Category { get; set; }

        public bool HaltOnCrash { get; set; }

        public bool CanExecute()
        {
            if (string.IsNullOrEmpty(Run) && string.IsNullOrEmpty(Category))
            {
                Log.WriteLine("Either a defined run or category is required.");
                return false;
            }
            return false;
        }

        public bool Execute()
        {
            // Add output file target
            // read result
            // write our middle file ? track?
            // injest with testresults call?
            
            return true;
        }
    }
}