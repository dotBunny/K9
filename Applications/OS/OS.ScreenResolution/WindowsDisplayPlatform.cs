// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Runtime.InteropServices;

namespace ScreenResolution;

public class WindowsDisplayPlatform
{
    // https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-devmodea
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODEA
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;

        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;

        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int ChangeDisplaySettingsA(ref DEVMODEA lpDevMode, uint dwFlags);

    public static bool SetResolution(int x = 1920, int y = 1080)
    {
        DEVMODEA dm = new DEVMODEA
        {
            dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODEA)),
            dmPelsWidth = (uint)x,
            dmPelsHeight = (uint)y,
            dmFields = 0x00080000 | 0x00100000
        };

        return ChangeDisplaySettingsA(ref dm, 0) == 0;
    }
}