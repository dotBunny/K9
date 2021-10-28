// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using K9.Unity.Verbs;
using K9.Utils;

namespace K9.Unity
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.UNITY";

        private static void Main(string[] args)
        {
            try
            {
                // Initialize Core
                Instance = new Program();
                Core.Init(Instance);

                Parser parser = new(Settings =>
                {
                    Settings.CaseInsensitiveEnumValues = true;
                    Settings.IgnoreUnknownArguments = true; // Allows for Wrapper to work
                });

                ParserResult<object> results = parser.ParseArguments<VersionControlSettings, TestResults, AddPackage, RemovePackage, Wrapper, Verbs.TestWrapper>(Core.Arguments);


                bool newResult = results.MapResult(
                    (VersionControlSettings vcs) => vcs.CanExecute() && vcs.Execute(),
                    (TestResults tests) => tests.CanExecute() && tests.Execute(),
                    (AddPackage addPackage) => addPackage.CanExecute() && addPackage.Execute(),
                    (RemovePackage removePackage) => removePackage.CanExecute() && removePackage.Execute(),
                    (Wrapper wrapper) => wrapper.CanExecute() && wrapper.Execute(),
                    (Verbs.TestWrapper runner) => runner.CanExecute() && runner.Execute(),
                    _ => false);

                if (!newResult)
                {
                    CommandLineUtil.HandleParserResults(results);
                }
            }
            catch (Exception e)
            {
                Core.ExceptionHandler(e);
            }
            finally
            {
                Core.Shutdown();
            }
        }
    }
}