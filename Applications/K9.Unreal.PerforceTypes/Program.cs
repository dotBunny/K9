// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using K9.Core;
using K9.Core.LogOutputs;
using K9.Core.Utils;
using K9.Services.Perforce;

namespace K9.Unreal.Types;

internal static class Program
{
    // ReSharper disable InconsistentNaming
    public enum FileType
    {
        Text,
        Binary,
        UTF8,
        UTF16,
        Symlink
    }
    // ReSharper restore InconsistentNaming


    public static string GetPerforceType(FileType type)
    {
        switch (type)
        {
            case FileType.UTF8:
                return "utf8";
            case FileType.UTF16:
                return "utf16";
            case FileType.Text:
                return "text";
            case FileType.Symlink:
                return "symlink";
        }
        return "binary";
    }


    struct WorkUnit(FileType type, string path)
    {
        public readonly FileType Type = type;
        public readonly string Path = path;
    }

    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {
            DefaultLogCategory = "UNREAL.TYPES",
            LogOutputs = [new ConsoleLogOutput()]
        });

        try
        {
            // Find our root
            string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();
            if (workspaceRoot == null)
            {
                Log.WriteLine("Unable to find workspace root.", ILogOutput.LogType.Error);
                framework.Environment.UpdateExitCode(1, true);
                return;
            }

            if (!framework.Arguments.HasOverrideArgument("changelist"))
            {
                Log.WriteLine("A changelist must be defined.", ILogOutput.LogType.Error);
                framework.Environment.UpdateExitCode(1, true);
                return;
            }
            string changelist = framework.Arguments.OverrideArguments["changelist"];

            // Try to standardize our file/locations, etc.
            SettingsProvider settings = new(workspaceRoot);

            Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "UnrealTypes"));
            settings.Output();

            string rootDirectory = workspaceRoot;
            if (framework.Arguments.HasOverrideArgument("directory"))
            {
                rootDirectory = framework.Arguments.OverrideArguments["directory"];
            }

            WorkUnit[] workUnits = FindUntypedFiles(rootDirectory);
            UpdateFileTypes(workspaceRoot, workUnits, changelist);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }


    static WorkUnit[] FindUntypedFiles(string rootDirectory)
    {
        Log.SetThreadSafeMode();
        System.Collections.Concurrent.ConcurrentBag<WorkUnit> workUnits = [];
        _ = Parallel.ForEach(Directory.EnumerateFiles(rootDirectory, "*.*", SearchOption.AllDirectories), path =>
        {
            byte[] bom = new byte[4];
            try
            {
                using FileStream file = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                file.ReadExactly(bom, 0, 4);

                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                {
                    workUnits.Add(new WorkUnit(FileType.UTF8, path));
                }

                if ((bom[0] == 0xff && bom[1] == 0xfe) || (bom[0] == 0xfe && bom[1] == 0xff))
                {
                    workUnits.Add(new WorkUnit(FileType.UTF16, path));
                }
            }
            catch (Exception)
            {
                Log.WriteLine($"Skipping {path} ...");
            }
        });

        Log.ClearThreadSafeMode();
        return workUnits.ToArray();
    }

    static void UpdateFileTypes(string workspaceRoot, WorkUnit[] files, string changelist)
    {
        // Turn on explicit thread safety for logging
        Log.SetThreadSafeMode();

        Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 32 }, file =>
        {
            WorkUnit currentUnit = file;
            string currentPath = $"\"{currentUnit.Path}\"";

            // Get current type
            string currentP4Type = GetPerforceType(currentUnit.Type);

            ProcessLogCapture outputCapture = new();

            // Why are we doing this?
            ProcessUtil.Execute("p4", workspaceRoot, $"files {currentPath}", null, outputCapture.GetAction());

            // Lets check its fstat
            outputCapture.Reset();
            ProcessUtil.Execute("p4", workspaceRoot, $"fstat {currentPath}", null, outputCapture.GetAction());
            if (!outputCapture.IsFirstLineEmpty() &&
                outputCapture.GetFirstLine().StartsWith("... type") &&
                outputCapture.GetFirstLine().Replace("... type", string.Empty).Trim() == currentP4Type)
            {
                    return;
            }

            // Check if the file is under the client's root
            if (outputCapture.GetString().Contains("is not under client's root"))
            {
                Log.WriteLine($"{currentUnit.Path} - is not under client's root.", ILogOutput.LogType.Error);
                return;
            }

            // The file is new to perforce
            if (outputCapture.GetString().Trim().EndsWith("no such file(s)."))
            {
                outputCapture.Reset();
                ProcessUtil.Execute("p4", workspaceRoot, $"add -t {currentP4Type} -c {changelist} {currentPath}",
                    null, outputCapture.GetAction());

                if (outputCapture.GetString().Trim().EndsWith("use 'reopen'"))
                {
                    ReopenFile(workspaceRoot, currentP4Type, changelist, currentPath, currentUnit.Path);
                }
                else
                {
                    Log.WriteLine($"Changed type on ADD ({currentP4Type}) of {currentUnit.Path}");
                }
            }
            else
            {
                ReopenFile(workspaceRoot, currentP4Type, changelist, currentPath, currentUnit.Path);
            }
        });

        // Add log items to unsafe items
        Log.ClearThreadSafeMode();
    }

    static void ReopenFile(string workspaceRoot, string currentP4Type, string changelist, string currentPath, string rawPath)
    {
        ProcessLogCapture outputCapture = new();
        ProcessUtil.Execute("p4", workspaceRoot, $"reopen -t {currentP4Type} -c {changelist} {currentPath}", null,
            outputCapture.GetAction());

        string processResponse = outputCapture.GetString().Trim();
        if (processResponse.EndsWith($"type {currentP4Type}; change {changelist}"))
        {
            Log.WriteLine($"Changed type ({currentP4Type}) of {rawPath}");
        }
        else if (processResponse.EndsWith($"reopened; change {changelist}"))
        {
            // Log.WriteLine($"NOOP type ({currentP4Type}) of {rawPath}");
        }
        else
        {
            Log.WriteLine($"Failed to change type ({currentP4Type}) of {rawPath}", ILogOutput.LogType.Error);
        }
    }
}