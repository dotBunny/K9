// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;

namespace K9.OS.ScreenResolution
{
    public class ScreenResolutionConfig
    {
        public int Width = 1920;
        public int Height = 1080;

        public static ScreenResolutionConfig Get(ConsoleApplication framework)
        {
            ScreenResolutionConfig config = new();

            if (framework.Arguments.HasOverrideArgument("HEIGHT"))
            {
                config.Height = int.Parse(framework.Arguments.OverrideArguments["HEIGHT"]);
            }

            if (framework.Arguments.HasOverrideArgument("WIDTH"))
            {
                config.Width = int.Parse(framework.Arguments.OverrideArguments["WIDTH"]);
            }
            return config;
        }
    }
}
