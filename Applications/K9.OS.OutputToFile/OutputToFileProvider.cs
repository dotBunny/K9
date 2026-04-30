// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.OutputToFile;

public class OutputToFileProvider : ProgramProvider
{
    public string? WorkingDirectory = null;
    public string Command = string.Empty;
    public string? Arguments;
    public string Target = string.Empty;

    public override string GetDescription()
    {
        return "Wrap execution of applications to control error handling and logging.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[4];

        lines[0] = new KeyValuePair<string, string>("WORKING-DIRECTORY", "Where the command should be executed. (Optional)");
        lines[1] = new KeyValuePair<string, string>("COMMAND", "The command to execute.");
        lines[2] = new KeyValuePair<string, string>("ARGUMENTS", "The arguments to pass to the command. (Optional)");
        lines[3] = new KeyValuePair<string, string>("TARGET", "The file to write the output too.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("COMMAND"))
        {
            Log.WriteLine("A COMMAND must be defined (---COMMAND=echo");
            return false;
        }

        if (!args.HasOverrideArgument("TARGET"))
        {
            Log.WriteLine("A COMMAND must be defined (---TARGET=c:\\output.txt");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Command = args.GetOverrideArgument("COMMAND");
        Target = args.GetOverrideArgument("TARGET");

        if (args.HasOverrideArgument("WORKING-DIRECTORY"))
        {
            WorkingDirectory = args.GetOverrideArgument("WORKING-DIRECTORY");
        }

        if (args.HasOverrideArgument("ARGUMENTS"))
        {
            Arguments = args.GetOverrideArgument("ARGUMENTS");
        }
    }
}