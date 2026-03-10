// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.ScreenResolution
{
    public class ScreenResolutionProvider : ProgramProvider
    {
        public int Width = 1920;
        public int Height = 1080;

        public override string GetDescription()
        {
            return "A tool to force a specific screen resolution.";
        }

        public override KeyValuePair<string, string>[] GetArgumentHelp()
        {
            KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

            lines[0] = new KeyValuePair<string, string>("WIDTH", "The screen pixel width to set.\n\t\t(Optional: 1920)");
            lines[1] = new KeyValuePair<string, string>("HEIGHT", "The screen pixel height to set.\n\t\t(Optional: 1080)");

            return lines;
        }

        public override void ParseArguments(ArgumentsModule args)
        {
            base.ParseArguments(args);

            if (args.HasOverrideArgument("WIDTH"))
            {
                Width = int.Parse(args.GetOverrideArgument("WIDTH"));
            }

            if (args.HasOverrideArgument("HEIGHT"))
            {
                Height = int.Parse(args.GetOverrideArgument("HEIGHT"));
            }
        }
    }
}
