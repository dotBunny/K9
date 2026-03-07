// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.ScreenResolution
{
    internal class Application
    {
        static void Main()
        {
            using ConsoleApplication framework = new(
            new K9.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "SCREENRESOLUTION",
                LogOutputs = [new K9.Core.Loggers.ConsoleLogOutput()]
            });

            try
            {
                ScreenResolutionConfig config = ScreenResolutionConfig.Get(framework);
                switch (framework.Platform.OperatingSystem)
                {
                    case PlatformModule.PlatformType.Windows:
                        if (WindowsDisplayPlatform.SetResolution(config.Width, config.Height))
                        {
                            Log.WriteLine($"Resolution changed to {config.Width}x{config.Height}.", ILogOutput.LogType.Info);
                        }
                        else
                        {
                            framework.ExceptionHandler(new Exception("Failed to set resolution."));
                        }
                        break;
                    default:
                        Log.WriteLine($"Unable to change resolution of unsupported platform.", ILogOutput.LogType.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }


}