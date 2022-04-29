// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using K9.Process.Verbs;
using K9.Utils;

namespace K9.Process
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.PROCESS";

        private static void Main(string[] args)
        {
            try
            {
                // Initialize Core
                Instance = new Program();
                Core.Init(Instance);

                Parser parser = Core.GetDefaultParser(true);

                ParserResult<object> results =
                    parser.ParseArguments<Start, Kill, Wait>(Core.Arguments);

                bool newResult = results.MapResult(
                    (Start start) => start.CanExecute() && start.Execute(),
                    (Kill kill) => kill.CanExecute() && kill.Execute(),
                    (Wait wait) => wait.CanExecute() && wait.Execute(),
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