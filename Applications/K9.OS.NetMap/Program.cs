// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;

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


        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}