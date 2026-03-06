// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;

namespace ScreenResolution
{
    public class ScreenResolutionConfig()
    {        
        public int Width = 1920;
        public int Height = 1080;


        public static ScreenResolutionConfig Get(ConsoleApplication framework)
        {
            ScreenResolutionConfig config = new ScreenResolutionConfig();

            if (framework.Arguments.OverrideArguments.ContainsKey("HEIGHT"))
            {
                config.Height = int.Parse(framework.Arguments.OverrideArguments["HEIGHT"]);               
            }
          
            if (framework.Arguments.OverrideArguments.ContainsKey("WIDTH"))
            {
                config.Width = int.Parse(framework.Arguments.OverrideArguments["WIDTH"]);
            }      
            return config;
        }
    }
}
