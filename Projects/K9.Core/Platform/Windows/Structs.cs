// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace K9.Platform.Windows
{
    public class Structs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public readonly IntPtr hProcess;
            public readonly IntPtr hThread;
            public readonly uint dwProcessId;
            public readonly uint dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public readonly string lpReserved;
            public string lpDesktop;
            public readonly string lpTitle;
            public readonly uint dwX;
            public readonly uint dwY;
            public readonly uint dwXSize;
            public readonly uint dwYSize;
            public readonly uint dwXCountChars;
            public readonly uint dwYCountChars;
            public readonly uint dwFillAttribute;
            public readonly uint dwFlags;
            public short wShowWindow;
            public readonly short cbReserved2;
            public readonly IntPtr lpReserved2;
            public readonly IntPtr hStdInput;
            public readonly IntPtr hStdOutput;
            public readonly IntPtr hStdError;
        }
    }
}