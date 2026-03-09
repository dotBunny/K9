// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.Modules;

namespace K9.OS.KeepAlive;

public class KeepAliveProvider : ProgramProvider
{
    public KeepAliveConfig? Config;

    public override bool IsValid(ArgumentsModule args)
    {
        string jsonPath = args.GetFirstArgument();
        if (!System.IO.File.Exists(jsonPath))
        {
            Log.WriteLine($"Unable to find provided config file @ {jsonPath}, returning empty data.", ILogOutput.LogType.Warning);
            return false;
        }

        KeepAliveConfig jsonConfig = KeepAliveConfig.Get(args.GetFirstArgument());
        if (!jsonConfig.IsValid())
        {
            return false;
        }

        return base.IsValid(args);
    }

    public override string GetDescription()
    {
        return
            "An application designed to keep a launched application running, much like a service. Simply provide a path to a KeepAliveConfig JSON file as the first argument.";
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        Config = KeepAliveConfig.Get(args.GetFirstArgument());
    }
}