// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;
using CommandLine.Text;

namespace K9.Utils
{
    public static class CommandLineUtil
    {
        public static void HandleParserResults<T>(ParserResult<T> results)
        {
            if (results.Tag == ParserResultType.NotParsed)
            {
                results.WithNotParsed(v =>
                {
                    foreach (Error e in v)
                    {
                        if (e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.HelpVerbRequestedError)
                        {
                            Log.WriteLine(HelpText.AutoBuild(results, _ => _, _ => _), Core.DefaultLogCategory, Log.LogType.Info);
                        }
                        else
                        {
                            Log.WriteLine($"{e.Tag} - No actions taken.", Core.DefaultLogCategory, Log.LogType.Error);
                        }
                    }
                });
            }
        }
    }
}