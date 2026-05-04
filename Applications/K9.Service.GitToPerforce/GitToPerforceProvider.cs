// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.Modules;

namespace K9.Service.GitToPerforce;

public class GitToPerforceProvider : ProgramProvider
{
    public GitToPerforceConfig? Config;

    public override bool IsValid(ArgumentsModule args)
    {
        // No argument use default value
        if (string.IsNullOrEmpty(args.GetFirstArgument()))
        {
            return true;
        }

        string jsonPath = args.GetFirstArgument();
        if (!System.IO.File.Exists(jsonPath))
        {
            Log.WriteLine($"Unable to find provided config file @ {jsonPath}, returning empty data.", ILogOutput.LogType.Warning);
            return false;
        }

        GitToPerforceConfig jsonConfig = GitToPerforceConfig.Get(args.GetFirstArgument());
        return jsonConfig.IsValid() && base.IsValid(args);
    }

    public override string GetDescription()
    {
        return "Synchronizes a git repository to a Perforce workspace.";
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Config = GitToPerforceConfig.Get(args.GetFirstArgument());
    }
}