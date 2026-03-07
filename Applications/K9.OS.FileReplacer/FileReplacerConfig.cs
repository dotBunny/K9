// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;

namespace K9.OS.FileReplacer;

public class FileReplacerConfig
{
    public string? SourceFile;
    public string? TargetFile;
    public readonly Dictionary<string, string> Replaces = new();

    public static FileReplacerConfig Get(ConsoleApplication framework)
    {
        FileReplacerConfig config = new();

        // HANDLE SOURCE
        if (framework.Arguments.HasOverrideArgument("SOURCE"))
        {
            config.SourceFile = framework.Arguments.OverrideArguments["SOURCE"];
            if (!File.Exists(config.SourceFile))
            {
                throw (new FileNotFoundException($"Unable to find source {config.SourceFile}"));
            }
        }
        else
        {
            throw (new Exception("A SOURCE file is required (---SOURCE=/my/input)"));
        }

        // Handle TARGET
        if (framework.Arguments.HasOverrideArgument("TARGET"))
        {
            config.TargetFile = framework.Arguments.OverrideArguments["TARGET"];
        }
        else
        {
            throw (new Exception("A TARGET file is required (---TARGET=/my/output)"));
        }

        // Build Replacement List
        if (framework.Arguments.HasOverrideArgument("KEY") && framework.Arguments.HasOverrideArgument("VALUE"))
        {
            string[] keys = framework.Arguments.OverrideArguments["KEY"].Split(",", StringSplitOptions.RemoveEmptyEntries);
            string[] values = framework.Arguments.OverrideArguments["VALUE"].Split(",", StringSplitOptions.RemoveEmptyEntries);

            if (keys.Length != values.Length)
            {
                throw (new Exception($"The number of KEYS({keys.Length}) does match the number of VALUES({values.Length})."));
            }

            for (int i = 0; i < keys.Length; i++)
            {
                config.Replaces.Add(keys[i], values[i]);
            }
        }
        else
        {
            throw (new Exception("Both KEY and VALUE are necessary (---KEY=a,b,c ---VALUE=1,2,3)"));
        }
        return config;
    }
}