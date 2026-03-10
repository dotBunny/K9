// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.DeleteFile;

public class DeleteFileProvider : ProgramProvider
{
    public string? TargetFile;

    public override bool IsValid(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("TARGET"))
        {
            if (!File.Exists(args.GetOverrideArgument("TARGET")))
            {
                Log.WriteLine($"Unable to find TARGET @ {args.GetOverrideArgument("TARGET")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("A TARGET file is required (---TARGET=/my/file)");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        TargetFile = args.GetOverrideArgument("TARGET");

        base.ParseArguments(args);
    }
}