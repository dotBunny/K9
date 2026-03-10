// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.WriteTextFile;

public class WriteTextFileProvider : ProgramProvider
{
    public string? Target;
    public string[]? Content;

    public override string GetDescription()
    {
        return "Copy a file to target folder, optionally extracting it.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[3];

        lines[0] = new KeyValuePair<string, string>("TARGET", "The target file to write to.");
        lines[1] = new KeyValuePair<string, string>("CONTENT", "The content you wish to write to the file. (Optional)");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("TARGET"))
        {
            Log.WriteLine("A TARGET file is required (---TARGET=/my/source/file)");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Target = args.GetOverrideArgument("TARGET");
        if (args.HasOverrideArgument("CONTENT"))
        {
            string workingContent = args.GetOverrideArgument("CONTENT")
                .Replace("___SPACE___", " ")
                .Replace("___TAB___", "\t")
                .Replace("___QUOTE___", "\"");

            Content = workingContent.Split(["\n", "___NEWLINE___"], StringSplitOptions.None);
        }

        base.ParseArguments(args);
    }
}