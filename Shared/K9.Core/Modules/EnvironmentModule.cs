// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;

namespace K9.Core.Modules;

public class EnvironmentModule : IModule
{
    public int ExitCode;
    public string? OriginalWorkingDirectory;

    public void UpdateExitCode(int code, bool forceSet = false)
    {
        if (forceSet)
        {
            ExitCode = code;
            return;
        }

        // This will update the exit code to the last known bad code
        if (code != 0)
        {
            ExitCode = code;
        }
    }

    public void Init(PlatformModule platform)
    {
        // ReSharper disable StringLiteralTypo
        Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        OriginalWorkingDirectory = Directory.GetCurrentDirectory();
        Environment.SetEnvironmentVariable("OriginalWorkingDirectory", OriginalWorkingDirectory);

        if (platform.OperatingSystem == PlatformModule.PlatformType.Windows)
        {
            Environment.SetEnvironmentVariable("Win32", @"C:\Windows\System32");
        }

        Environment.SetEnvironmentVariable("COMPUTERNAME", Environment.MachineName);
        // ReSharper restore StringLiteralTypo
    }
}