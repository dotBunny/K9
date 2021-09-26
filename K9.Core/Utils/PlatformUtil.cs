using System.Runtime.InteropServices;

namespace K9.Services.Utils
{
    public static class PlatformUtil
    {
        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static bool IsMacOS()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        public static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        public static int GetBlockSize(string path = null)
        {
            // if (string.IsNullOrEmpty(path)) return 4096;
            return 4096;
        }
    }
}