// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using static K9.Core.ILogOutput;
namespace K9.Core.LogOutputs;

public class ConsoleLogOutput : ILogOutput
{
    const ConsoleColor k_DefaultForegroundColor = ConsoleColor.Gray;
    const ConsoleColor k_DefaultBackgroundColor = ConsoleColor.Black;

    public void LineFeed()
    {
        Console.WriteLine();
    }

    public void Shutdown()
    {
        Console.ResetColor();
    }

    public void WriteLine(LogType logType, string message)
    {
        Console.ForegroundColor = logType switch
        {
            LogType.Notice => ConsoleColor.DarkGreen,
            LogType.Error => ConsoleColor.DarkRed,
            LogType.Info => ConsoleColor.DarkCyan,
            LogType.Warning => ConsoleColor.DarkYellow,
            LogType.ExternalProcess => ConsoleColor.DarkGray,
            _ => k_DefaultForegroundColor,
        };
        Console.WriteLine(message);
        Console.ForegroundColor = k_DefaultForegroundColor;
    }

    public void RestoreDefaultColors()
    {
        Console.ForegroundColor = k_DefaultForegroundColor;
        Console.BackgroundColor = k_DefaultBackgroundColor;
    }

    public bool IsThreadSafe()
    {
        return true;
    }
}