// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using static K9.Core.ILogOutput;

namespace K9.Core.Loggers
{
    public class ConsoleLogOutput : ILogOutput
    {

        public ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;
        public ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;

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
                _ => DefaultForegroundColor,
            };
            Console.WriteLine(message);

            Console.ForegroundColor = DefaultForegroundColor;
        }

        public bool IsThreadSafe()
        {
            return true;
        }
    }
}
