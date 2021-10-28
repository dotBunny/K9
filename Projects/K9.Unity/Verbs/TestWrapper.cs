// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;
using K9.Unity.TestRunner;
using Newtonsoft.Json;

namespace K9.Unity.Verbs
{
    [Verb("TestWrapper")]
    public class TestWrapper : IVerb
    {

        /*
        "C:\Program Files\Unity\Hub\Editor/2020.3.14f1-dots.0.spotlight.0/Editor/Unity.exe"
        -projectPath
        .
        -runTests
        -testPlatform
        editmode
        -testResults
        D:\BuildAgent\temp\buildTmp/unityTestsResults-EditMode-16.xml
        -editorTestsCategories
        XP.Bonfire.Tests.Editor
        -accept-apiupdate
        -EnableCacheServer */

        [Option('d', "definition", Required = false, HelpText = "")]
        public string Definition { get; set; }

        [Option('p', "testPlatform", Required = false, Default = "editmode", HelpText = "")]
        public string Platform { get; set; }

        [Option('o', "testResults", Required = false, HelpText = "")]
        public string TestResults { get; set; }

        [Option('f', "testFilter", Required = false, HelpText = "")]
        public string TestFilters { get; set; }


        [Option('c', "editorTestsCategories", Required = false, HelpText = "")]
        public string Category { get; set; }

        [Option('h', "halt", Required = false, HelpText = "Halt test run if crash detected.")]
        public bool HaltOnCrash { get; set; }


        private SuiteDefinition _suiteDefinition;
        public bool CanExecute()
        {
            if (string.IsNullOrEmpty(Definition) && string.IsNullOrEmpty(Category))
            {
                Log.WriteLine("Either a defined run or category is required.");
                return false;
            }

            if (!string.IsNullOrEmpty(Definition))
            {
                if (System.IO.File.Exists(Definition))
                {
                    _suiteDefinition = JsonConvert.DeserializeObject<SuiteDefinition>(System.IO.File.ReadAllText(Definition));
                    if (_suiteDefinition != null)
                    {
                        return true;
                    }
                    Log.WriteLine($"Unable to parse {Definition}.");
                }
                else
                {
                    Log.WriteLine($"Unable to find {Definition}.");
                }

                return false;
            }


            return true;
        }

        public bool Execute()
        {
            // -forgetProjectPath

            // Add -runTests
            // if m - use testPlaform


            // Add output file target
            // read result
            // write our middle file ? track?
            // injest with testresults call?

            return true;
        }


    }
}