// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace K9.Platform.Windows
{
    [SupportedOSPlatform("windows")]
    public static class Impersonate
    {
        public static bool RunImpersonated(string username, string domain, string password, Action action)
        {
            // Attempt to login to the user
            bool returnValue = Win32.LogonUser(username, domain, password, Constant.LOGON32_LOGON_INTERACTIVE,
                Constant.LOGON32_PROVIDER_DEFAULT, out SafeAccessTokenHandle safeAccessTokenHandle);

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

        public static uint StartProcessAsUser(string username, string domain, string password, string appPath,
            string cmdLine = null, string workDir = null,
            bool visible = true)
        {
            bool loggedIn = Win32.LogonUser(username, domain, password, Constant.LOGON32_LOGON_INTERACTIVE,
                Constant.LOGON32_PROVIDER_DEFAULT, out SafeAccessTokenHandle safeAccessTokenHandle);

            if (!loggedIn)
            {
                // TODO: Error
                return Constant.INVALID_PROCESS_ID;
            }

            IntPtr userToken = safeAccessTokenHandle.DangerousGetHandle();

            Structs.STARTUPINFO startInfo = new();
            Structs.PROCESS_INFORMATION procInfo = new();
            IntPtr pEnv = IntPtr.Zero;
            int iResultOfCreateProcessAsUser;

            startInfo.cb = Marshal.SizeOf(typeof(Structs.STARTUPINFO));

            try
            {
                uint dwCreationFlags =
                    Constant.CREATE_UNICODE_ENVIRONMENT | (uint)(visible ? Constant.CREATE_NEW_CONSOLE : Constant.CREATE_NO_WINDOW);
                startInfo.wShowWindow = (short)(visible ? Enumerations.SW.SW_SHOW : Enumerations.SW.SW_HIDE);
                startInfo.lpDesktop = "winsta0\\default";

                if (!Win32.CreateEnvironmentBlock(ref pEnv, userToken, false))
                {
                    throw new Exception("StartProcessAsCurrentUser: CreateEnvironmentBlock failed.");
                }

                if (!Win32.CreateProcessAsUser(userToken,
                    appPath, // Application Name
                    cmdLine, // Command Line
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    dwCreationFlags,
                    pEnv,
                    workDir, // Working directory
                    ref startInfo,
                    out procInfo))
                {
                    iResultOfCreateProcessAsUser = Marshal.GetLastWin32Error();
                    throw new Exception("StartProcessAsCurrentUser: CreateProcessAsUser failed.  Error Code -" +
                                        iResultOfCreateProcessAsUser);
                }
            }
            finally
            {
                //CloseHandle(identity.Token);
                if (pEnv != IntPtr.Zero)
                {
                    Win32.DestroyEnvironmentBlock(pEnv);
                }

                Win32.CloseHandle(procInfo.hThread);
                Win32.CloseHandle(procInfo.hProcess);
            }

            return procInfo.dwProcessId;
        }
    }
}