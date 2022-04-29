// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using K9.TeamCity.Verbs;
using K9.Utils;

namespace K9.TeamCity
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.TEAMCITY";

        private static void Main(string[] args)
        {
            try
            {
                // Initialize Core
                Instance = new Program();
                Core.Init(Instance);

                Parser parser = Core.GetDefaultParser();

                ParserResult<object> results = parser.ParseArguments<BuildChangelist, SetParameter, CompareImage>(Core.Arguments);

                bool newResult = results.MapResult(
                    (BuildChangelist changelist) => changelist.CanExecute() && changelist.Execute(),
                    (SetParameter param) => param.CanExecute() && param.Execute(),
                    (CompareImage compareImage) => compareImage.CanExecute() && compareImage.Execute(),
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