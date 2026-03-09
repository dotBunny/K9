// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.ScreenResolution;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {

            // ReSharper disable once StringLiteralTypo
            DefaultLogCategory = "OS.SCREENRESOLUTION",
            LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
        },
        new ScreenResolutionProvider());

        try
        {
            ScreenResolutionProvider provider = (ScreenResolutionProvider)framework.ProgramProvider;

            switch (framework.Platform.OperatingSystem)
            {
                case PlatformModule.PlatformType.Windows:
                    if (WindowsDisplayPlatform.SetResolution(provider.Width, provider.Height))
                    {
                        Log.WriteLine($"Resolution changed to {provider.Width}x{provider.Height}.", ILogOutput.LogType.Info);
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