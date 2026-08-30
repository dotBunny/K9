// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace K9.Core.Utils;

public static class CompressionUtil
{
    // Tuned for ratio over speed; a solid LZMA2 archive with a 64m dictionary across all cores.
    public const string DefaultSevenZipArguments = "-t7z -m0=lzma2 -mx=9 -mfb=64 -md=64m -ms=on -mmt=on";

    static string? s_SevenZipPath;
    static bool s_SevenZipSearched;

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

    public static bool Create7z(string sourceFolder, string sourceString, string targetPath,
        string? sevenZipPath = null, string? argumentOverride = null)
    {
        string? executablePath = GetSevenZipPath(sevenZipPath);
        if (executablePath == null)
        {
            Log.WriteLine("Unable to find a 7-Zip executable; install 7-Zip or provide an explicit path.",
                ILogOutput.LogType.Error);
            return false;
        }

        // 7-Zip updates an existing archive instead of replacing it, which is not what a create implies.
        if (File.Exists(targetPath))
        {
            Log.WriteLine($"Removing existing archive at {targetPath}.", ILogOutput.LogType.Warning);
            FileUtil.ForceDeleteFile(targetPath);
        }
        FileUtil.EnsureFileFolderHierarchyExists(targetPath);

        string switches = string.IsNullOrEmpty(argumentOverride) ? DefaultSevenZipArguments : argumentOverride;

        ProcessLogRedirect logRedirect = new();
        int returnCode = ProcessUtil.Execute(executablePath, sourceFolder,
            $"a {switches} -y -bsp0 \"{targetPath}\" \"{sourceString}\"", null, logRedirect.GetAction());

        return EvaluateSevenZipExitCode(returnCode, "creating");
    }

    public static bool Test7z(string archivePath, string? sevenZipPath = null)
    {
        string? executablePath = GetSevenZipPath(sevenZipPath);
        if (executablePath == null)
        {
            Log.WriteLine("Unable to find a 7-Zip executable; install 7-Zip or provide an explicit path.",
                ILogOutput.LogType.Error);
            return false;
        }

        ProcessLogRedirect logRedirect = new();
        int returnCode = ProcessUtil.Execute(executablePath, null, $"t -y -bsp0 \"{archivePath}\"", null,
            logRedirect.GetAction());

        return EvaluateSevenZipExitCode(returnCode, "testing");
    }

    public static bool Extract7z(string sourcePath, string targetFolder, string? sevenZipPath = null)
    {
        string? executablePath = GetSevenZipPath(sevenZipPath);
        if (executablePath == null)
        {
            Log.WriteLine("Unable to find a 7-Zip executable; install 7-Zip or provide an explicit path.",
                ILogOutput.LogType.Error);
            return false;
        }

        FileUtil.EnsureFolderHierarchyExists(targetFolder);

        ProcessLogRedirect logRedirect = new();
        int returnCode = ProcessUtil.Execute(executablePath, null,
            $"x -y -bsp0 \"-o{targetFolder}\" \"{sourcePath}\"", null, logRedirect.GetAction());

        return EvaluateSevenZipExitCode(returnCode, "extracting");
    }

    public static bool Extract(string sourcePath, string? targetFolder)
    {
        if (targetFolder == null)
        {
            Log.WriteLine("No target folder specified.", ILogOutput.LogType.Error);
            return false;
        }

        ProcessLogRedirect logRedirect = new();
        int returnCode;

        if (!IsSupported(sourcePath))
        {
            Log.WriteLine($"Unsupported compression based on extension: {Path.GetExtension(sourcePath)}", ILogOutput.LogType.Warning);
            return false;
        }

        // Only 7-Zip knows how to open its own format.
        if (Path.GetExtension(sourcePath).ToLower() == ".7z")
        {
            return Extract7z(sourcePath, targetFolder);
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
        return Path.GetExtension(sourcePath).ToLower() is ".zip" or ".tar.gz" or ".tar.bz2" or ".tar" or ".gz" or ".bz2" or ".7z";
    }

    public static string? GetSevenZipPath(string? overridePath = null)
    {
        // An explicitly configured path is never cached, nor guessed at; it works or it is an error.
        if (!string.IsNullOrEmpty(overridePath))
        {
            if (File.Exists(overridePath))
            {
                return overridePath;
            }

            Log.WriteLine($"The provided 7-Zip executable was not found @ {overridePath}.", ILogOutput.LogType.Error);
            return null;
        }

        if (s_SevenZipSearched)
        {
            return s_SevenZipPath;
        }
        s_SevenZipSearched = true;

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string[] executableNames = isWindows ? ["7z.exe", "7za.exe"] : ["7z", "7zz", "7za"];

        // Walk the PATH first so a purposefully installed version wins.
        string? pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVariable))
        {
            foreach (string searchFolder in pathVariable!.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(searchFolder))
                {
                    continue;
                }

                foreach (string executableName in executableNames)
                {
                    string candidate = Path.Combine(searchFolder.Trim(), executableName);
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }

                    s_SevenZipPath = candidate;
                    return s_SevenZipPath;
                }
            }
        }

        // Fallback to the well-known install locations.
        string[] knownPaths = isWindows
            ?
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
            ]
            : ["/usr/bin/7z", "/usr/bin/7zz", "/usr/local/bin/7z", "/usr/local/bin/7zz", "/opt/homebrew/bin/7z"];

        foreach (string knownPath in knownPaths)
        {
            if (!File.Exists(knownPath))
            {
                continue;
            }

            s_SevenZipPath = knownPath;
            return s_SevenZipPath;
        }

        return null;
    }

    static bool EvaluateSevenZipExitCode(int returnCode, string operation)
    {
        switch (returnCode)
        {
            case 0:
                return true;
            case 1:
                // Locked or in-use files are routine when working against a live workspace.
                Log.WriteLine($"7-Zip reported warnings while {operation} the archive; some files were skipped.",
                    ILogOutput.LogType.Warning);
                return true;
            case 2:
                Log.WriteLine($"7-Zip hit a fatal error while {operation} the archive.", ILogOutput.LogType.Error);
                return false;
            case 7:
                Log.WriteLine($"7-Zip rejected the command line while {operation} the archive.", ILogOutput.LogType.Error);
                return false;
            case 8:
                Log.WriteLine($"7-Zip ran out of memory while {operation} the archive.", ILogOutput.LogType.Error);
                return false;
            case 255:
                Log.WriteLine($"7-Zip was stopped by the user while {operation} the archive.", ILogOutput.LogType.Error);
                return false;
            default:
                Log.WriteLine($"7-Zip returned an unknown exit code ({returnCode}) while {operation} the archive.",
                    ILogOutput.LogType.Error);
                return false;
        }
    }
}
