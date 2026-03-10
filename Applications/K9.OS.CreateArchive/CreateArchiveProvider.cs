// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.CreateArchive;

public class CreateArchiveProvider : ProgramProvider
{
    public string Source = "*.*";
    public string? SourceFolder;
    public string? Target;

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("SOURCE-FOLDER"))
        {
            Log.WriteLine("A SOURCE-FOLDER  is required (---SOURCE=/my/source/)");
            return false;
        }

        if (!Directory.Exists(args.GetOverrideArgument("SOURCE-FOLDER")))
        {
            Log.WriteLine($"Unable to find SOURCE-FOLDER @ {args.GetOverrideArgument("SOURCE-FOLDER")}", ILogOutput.LogType.Warning);
            return false;
        }

        if (!args.HasOverrideArgument("TARGET"))
        {
            Log.WriteLine("A TARGET is required (---TARGET=/my/path/target.tar.gz)");
            return false;
        }

        if (File.Exists(args.GetOverrideArgument("TARGET")) && !args.HasBaseArgument("OVERWRITE"))
        {
            Log.WriteLine($"Unable to overwrite existing file {args.GetOverrideArgument("TARGET")}, use OVERWRITE flag.", ILogOutput.LogType.Warning);
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        SourceFolder = args.GetOverrideArgument("SOURCE-FOLDER");
        if (args.HasOverrideArgument("SOURCE"))
        {
            Source = args.GetOverrideArgument("SOURCE");
        }

        Target = args.GetOverrideArgument("TARGET");
    }

    public override string GetDescription()
    {
        return "Create an archive.";
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[1];

        lines[0] = new KeyValuePair<string, string>("OVERWRITE", "If the archive file already exists, overwrite it.");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[3];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The file filter to apply when adding files to the archive. (Optional: *.*)");
        lines[1] = new KeyValuePair<string, string>("SOURCE-FOLDER", "The base folder to start adding files to the archive from.");
        lines[2] = new KeyValuePair<string, string>("TARGET", "The absolute path of the archive file to create.");

        return lines;
    }
}