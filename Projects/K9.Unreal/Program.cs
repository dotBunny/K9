﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using K9.Unreal.Verbs;
using K9.Utils;

namespace K9.Unreal
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.UNREAL";

        private static void Main(string[] args)
        {
            try
            {
                // Initialize Core
                Instance = new Program();
                Core.Init(Instance);

                Parser parser = Core.GetDefaultParser();

                ParserResult<UERunTestsJSONToNUnitXML> results =
                    parser.ParseArguments<UERunTestsJSONToNUnitXML>(Core.Arguments);

                bool newResult = results.MapResult(
                    vcs => vcs.CanExecute() && vcs.Execute(),
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