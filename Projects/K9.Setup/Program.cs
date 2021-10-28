// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using K9.Setup.Verbs;
using K9.Utils;

namespace K9.Setup
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.SETUP";

        private static void Main(string[] args)
        {
            try
            {
                // Initialize Core
                Instance = new Program();
                Core.Init(Instance);

                Parser parser = new(Settings => Settings.CaseInsensitiveEnumValues = true);

                ParserResult<object> results =
                    parser.ParseArguments<Perforce, SetEnvironmentVariable, WriteFile, DeleteFolder, CopyFile, Checkout>(
                        Core.Arguments);

                bool newResult = results.MapResult(
                    (Perforce perforce) => perforce.CanExecute() && perforce.Execute(),
                    (SetEnvironmentVariable env) => env.CanExecute() && env.Execute(),
                    (WriteFile write) => write.CanExecute() && write.Execute(),
                    (DeleteFolder delete) => delete.CanExecute() && delete.Execute(),
                    (CopyFile copy) => copy.CanExecute() && copy.Execute(),
                    (Checkout checkout) => checkout.CanExecute() && checkout.Execute(),
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