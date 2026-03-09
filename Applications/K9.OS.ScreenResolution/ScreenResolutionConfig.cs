// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.Modules;

namespace K9.OS.ScreenResolution
{
    public class ScreenResolutionConfig : ProgramConfig
    {
        public int Width = 1920;
        public int Height = 1080;

        public override void Parse(ArgumentsModule args)
        {
            base.Parse(args);

            if (args.HasOverrideArgument("HEIGHT"))
            {
                Height = int.Parse(args.OverrideArguments["HEIGHT"]);
            }

            if (args.HasOverrideArgument("WIDTH"))
            {
                Width = int.Parse(args.OverrideArguments["WIDTH"]);
            }
        }
    }
}
