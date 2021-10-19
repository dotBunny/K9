// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;

namespace K9
{
    public static class Log
    {

        public enum LogType
        {
            Default,
            Notice,
            Info,
            ExternalProcess,
            Error
        }

        private const string DateStampFormat = "yyyy-MM-dd HH:mm:ss";
        private const int FixedCategoryLength = 12;
        public static ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;
        public static ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;

        public static void SetForegroundColor(LogType logType)
        {
            switch (logType)
            {
                case LogType.Notice:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case LogType.ExternalProcess:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    Console.ForegroundColor = DefaultForegroundColor;
                    break;
            }
        }
        public static void WriteLine(string output, string category = "DEFAULT", LogType logType = LogType.Default)
        {
            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            SetForegroundColor(logType);
            Console.WriteLine(
                $"[{DateTime.Now.ToString(DateStampFormat)}] {category.ToUpper().PadLeft(FixedCategoryLength, ' ')} > {output}");
            Console.ForegroundColor = DefaultForegroundColor;
        }

        public static void WriteRaw(string output, LogType logType = LogType.Default)
        {
            SetForegroundColor(logType);
            Console.WriteLine(output);
            Console.ForegroundColor = DefaultForegroundColor;
        }

        public static void Write(string output, LogType logType = LogType.Default)
        {
            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            SetForegroundColor(logType);
            Console.Write(output);
            Console.ForegroundColor = DefaultForegroundColor;
        }

        public static void LineFeed()
        {
            Console.WriteLine();
        }
    }
}