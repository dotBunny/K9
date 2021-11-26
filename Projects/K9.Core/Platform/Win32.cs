// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace K9.Platform
{
    [SupportedOSPlatform("windows")]
    public static class Win32
    {
        private const int LOGON32_PROVIDER_DEFAULT = 0;
        private const int LOGON32_LOGON_INTERACTIVE = 2;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string username, string domain, string password, int logonType,
            int logonProvider, out SafeAccessTokenHandle token);

        public static bool RunImpersonated(string username, string domain, string password, Action action)
        {
            // Attempt to login to the user
            bool returnValue = LogonUser(username, domain, password, LOGON32_LOGON_INTERACTIVE,
                LOGON32_PROVIDER_DEFAULT, out SafeAccessTokenHandle safeAccessTokenHandle);

            if (returnValue)
            {
                WindowsIdentity.RunImpersonated(safeAccessTokenHandle, action);
            }
            else
            {
                Log.WriteLine($"Unable to login to the provided user ({username}).", "WIN32", Log.LogType.Error);
            }
            return returnValue;
        }
    }
}