// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace K9.Core.Utils;

public static class CompressionUtil
{
    public static  bool Create(string sourceFolder, string sourceString, string targetPath)
    {
        ProcessLogRedirect logRedirect = new();
        int returnCode;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            returnCode = ProcessUtil.Execute("tar.exe", sourceFolder, $"-czf {targetPath} {sourceString}", null, logRedirect.GetAction());
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            returnCode = ProcessUtil.Execute("ditto", sourceFolder, $"-c {sourceString} {targetPath}", null, logRedirect.GetAction());
        }
        else
        {
            returnCode = ProcessUtil.Execute("tar", sourceFolder, $"-czf {targetPath} {sourceString}", null, logRedirect.GetAction());
        }

        return returnCode == 0;
    }
    public static bool Extract(string sourcePath, string targetFolder)
    {
        ProcessLogRedirect logRedirect = new();
        int returnCode;

        if (!IsSupported(sourcePath))
        {
            Log.WriteLine($"Unsupported compression based on extension: {Path.GetExtension(sourcePath)}", ILogOutput.LogType.Warning);
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            returnCode = ProcessUtil.Execute("tar.exe", targetFolder, $"-xf {sourcePath} -C {targetFolder}", null, logRedirect.GetAction());
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            returnCode = ProcessUtil.Execute("ditto", targetFolder, $"-x -k {sourcePath} {targetFolder}", null, logRedirect.GetAction());
        }
        else
        {
            returnCode = ProcessUtil.Execute("tar", targetFolder, $"-xf {sourcePath} -C {targetFolder}", null, logRedirect.GetAction());
        }

        return returnCode == 0;
    }

    public static bool IsSupported(string sourcePath)
    {
        return Path.GetExtension(sourcePath).ToLower() is ".zip" or ".tar.gz" or ".tar.bz2" or ".tar" or ".gz" or ".bz2";
    }
}