// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.NetMap;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "NETMAP",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new NetMapProvider());

        try
        {
            NetMapProvider provider = (NetMapProvider)framework.ProgramProvider;
            if(provider.NetworkUsername == null || provider.NetworkPassword == null)
            {
                Log.WriteLine("The NETWORK-USERNAME or NETWORK-PASSWORD is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            if (Directory.Exists(provider.NetworkMapping))
            {
                Log.WriteLine($"The NETWORK-MAPPING was reachable. Skipping...");
                framework.Shutdown();
                return;
            }

            ProcessLogRedirect logRedirect = new();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ProcessUtil.Execute("net", null,
                    $"use {provider.NetworkMapping} {provider.NetworkShare} /USER:{provider.NetworkUsername} {provider.NetworkPassword}",
                    null, logRedirect.GetAction());
            }
            else
            {
                // TODO: Not sure this works
                ProcessUtil.Execute("sudo mount", null,
                    $"mount -t cifs -o username={provider.NetworkUsername} password={provider.NetworkPassword} {provider.NetworkShare} {provider.NetworkMapping}",
                    null, logRedirect.GetAction());
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}