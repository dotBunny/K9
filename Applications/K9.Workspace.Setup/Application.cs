// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Loggers;
using K9.Core.Services.Git;
using K9.Core.Utils;
using K9.Services.Perforce;

namespace K9.Workspace.Setup;

static class Application
{
    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {
            DefaultLogCategory = "WORKSPACE.SETUP",
            LogOutputs = [new ConsoleLogOutput()],
            PauseOnExit = true,
            RequiresElevatedAccess = true,
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

            // Try to standardize our file/locations, etc.
            SettingsProvider settings = new(workspaceRoot);


            Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "K9.Workspace.Setup"));
            settings.Output();

            UpdateSourceCode(framework, settings);
            BuildSource(framework, settings);
            SetupEnvironment(settings);
            SetupPerforce(settings);
            SetupVSCode(settings);
            SetupExecutionFlags(framework, settings);
            SetupSecurityExclusions(framework, settings);
            SetupUnrealEngine(framework, settings);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }

    #region Process
    static void UpdateSourceCode(ConsoleApplication framework, SettingsProvider settings)
    {
        if (framework.Arguments.BaseArguments.Contains("no-source"))
        {
            Log.WriteLine("Skipping Source Check (Argument) ...", "SOURCE");
            return;
        }

        string? branch = GitProvider.GetBranch(settings.K9ToolboxFolder);
        branch ??= "main";

        string localCommitHash = GitProvider.GetLocalCommit(settings.K9ToolboxFolder);
        string? remoteCommitHash = GitProvider.GetRemoteCommit(settings.K9ToolboxFolder, branch);

        if (localCommitHash == remoteCommitHash)
        {
            Log.WriteLine($"Local repository up to date ({localCommitHash}).", "SOURCE");
        }
        else
        {
            Log.WriteLine($"Depot needs updating as the local {localCommitHash} differs from {remoteCommitHash}.", "SOURCE", ILogOutput.LogType.Info);
#if DEBUG
            Log.WriteLine("Skipping Cloning (Debug Mode) ...");
#else
            GitProvider.UpdateRepo(settings.K9ToolboxFolder, branch);
#endif
        }
    }

    static void BuildSource(ConsoleApplication framework, SettingsProvider settings)
    {
        if (framework.Arguments.BaseArguments.Contains("no-build"))
        {
            Log.WriteLine("Skipping Build Check (Argument) ...", "BUILD");
            return;
        }

        string localCommitHash = GitProvider.GetLocalCommit(settings.K9ToolboxFolder);
        string builtTagFile = Path.Combine(settings.K9ToolboxFolder, SettingsProvider.BuildHashFileName);
        bool shouldRebuild = !File.Exists(builtTagFile);
        if (!shouldRebuild)
        {
            shouldRebuild = (File.ReadAllText(builtTagFile).Trim() != localCommitHash);
        }

        if (shouldRebuild)
        {
            Log.WriteLine($"A rebuild of programs is needed.", "BUILD", ILogOutput.LogType.Notice);
#if DEBUG
            Log.WriteLine("Skipping Building (Debug Mode) ...");
#else
            ProcessUtil.SpawnSeperate("dotnet", $"{settings.BoostrapLibrary} quiet", null, true);
            framework.Shutdown(true);
#endif
        }
    }

    static void SetupPerforce(SettingsProvider settings)
    {
        Log.WriteLine("Setup Perforce", ILogOutput.LogType.Notice);

        Log.WriteLine("Set P4 Flags ...");
        ProcessUtil.SpawnHidden(PerforceProvider.GetExecutablePath(), $"set P4IGNORE={SettingsProvider.P4IgnoreFileName} P4CONFIG={SettingsProvider.P4ConfigFileName} P4CHARSET={SettingsProvider.P4CharacterSet}");

        Log.WriteLine($"Configure P4Config ...");
        if (!File.Exists(settings.P4ConfigFile))
        {
            Log.WriteLine($"Writing default P4Config ...");
            PerforceConfig.WriteDefault(settings.P4ConfigFile, SettingsProvider.P4Port, SettingsProvider.P4CharacterSet, SettingsProvider.P4IgnoreFileName);
            Log.WriteLine($"Opening P4Config for edit.");
            ProcessUtil.OpenFileWithDefault(settings.P4ConfigFile);
        }
        else
        {
            // We're not going to overwrite, but maybe we should validate?
            Log.WriteLine($"Existing P4Config was found.");
        }

        Log.WriteLine("Install P4V Tools ...");
        CustomTools.CustomToolDefList baseCustomTools = CustomTools.Get();

        // We need to find all the extra tools throughout the workspace
        List<string> p4Tools =
        [
            .. Directory.GetFiles(settings.K9Folder, SettingsProvider.P4CustomToolsFileName, SearchOption.AllDirectories),
            .. Directory.GetFiles(settings.ProjectsFolder, SettingsProvider.P4CustomToolsFileName, SearchOption.AllDirectories),
        ];
        int foundTools = p4Tools.Count;
        for (int i = 0; i < foundTools; i++)
        {
            string toolPath = p4Tools[i];
            Log.WriteLine($"Adding {toolPath} ...");
            CustomTools.CustomToolDefList customTools = CustomTools.Get(toolPath);
            baseCustomTools.AddOrReplace(customTools);
        }
        if (foundTools > 0)
        {
            baseCustomTools.Output(CustomTools.ConfigFile);
        }

    }
    static void SetupEnvironment(SettingsProvider settings)
    {
        bool restartShellsRequired = false;
        Log.WriteLine("Setup Environment", ILogOutput.LogType.Notice);

        string? existingMachinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
        if (existingMachinePath != null && !existingMachinePath.Contains(settings.K9DotNETFolder))
        {
            Log.WriteLine($"Adding to PATH variable ...");
            Environment.SetEnvironmentVariable("PATH", $"{existingMachinePath};{settings.K9DotNETFolder};", EnvironmentVariableTarget.Machine);
            restartShellsRequired = true;
        }

        if (restartShellsRequired)
        {
            Log.WriteLine("Restarting of terminals required to pickup new environment variables.", ILogOutput.LogType.Info);
        }

        ProcessLogOutput processLogOutput = new(ILogOutput.LogType.ExternalProcess);

        ProcessUtil.Execute("dotnet", settings.RootFolder, "dev-certs https --trust", null, processLogOutput.GetAction());

        // Update workloads
        ProcessUtil.Execute("dotnet", settings.RootFolder, "workload update", null, processLogOutput.GetAction());
    }

    // ReSharper disable once InconsistentNaming
    static void SetupVSCode(SettingsProvider settings)
    {
        Log.WriteLine("Setup VSCode", ILogOutput.LogType.Notice);
        string vscodePath = Path.Combine(settings.RootFolder, ".vscode", "settings.json");
        if (!File.Exists(vscodePath))
        {
            FileUtil.EnsureFileFolderHierarchyExists(vscodePath);
            string k9Path = Path.Combine(settings.K9DotNETFolder, "K9.exe").Replace("\\", "\\\\");
            File.WriteAllLines(vscodePath, [
                "{",
                    $"\t\"terminal.integrated.cwd\": \"{settings.K9DotNETFolder.Replace("\\", "\\\\")}\",",
                    "\t\"terminal.integrated.profiles.windows\": {",
                        "\t\t\"Command Prompt\": {",
                            $"\t\t\t\"args\": [\"/K\", \"{k9Path}\"]",
                        "\t\t},",
                        "\t\t\"PowerShell\": {",
                            $"\t\t\t\"args\": [\"-NoExit\", \"{k9Path}\"]",
                        "\t\t}",
                    "\t}",
                "}"
            ]);
        }
    }
    static void SetupExecutionFlags(ConsoleApplication framework, SettingsProvider settings)
    {
        Log.WriteLine("Ensuring Execution Flags", ILogOutput.LogType.Notice);
        // Ensure executable flags are setup across the workspace
        switch (framework.Platform.OperatingSystem)
        {
            case Core.Modules.PlatformModule.PlatformType.macOS:
            case Core.Modules.PlatformModule.PlatformType.Linux:
                string[] shFiles = Directory.GetFiles(settings.RootFolder, "*.sh", SearchOption.AllDirectories);
                string[] commandFiles = Directory.GetFiles(settings.RootFolder, "*.command", SearchOption.AllDirectories);

                foreach (string s in shFiles)
                {
                    ProcessUtil.SpawnHidden("chmod", $"+x {s}");
                }
                foreach (string c in commandFiles)
                {
                    ProcessUtil.SpawnHidden("chmod", $"+x {c}");
                }
                break;
        }
    }
    static void SetupSecurityExclusions(ConsoleApplication framework, SettingsProvider settings)
    {
        Log.WriteLine("Adding Security Exclusions", ILogOutput.LogType.Notice);
        switch (framework.Platform.OperatingSystem)
        {
            case Core.Modules.PlatformModule.PlatformType.Windows:
                ProcessUtil.Elevate("powershell", settings.RootFolder,
                    $"-inputformat none -outputformat none -NonInteractive -Command Add-MpPreference -ExclusionPath \"{settings.RootFolder}\"");
                break;
        }
    }

    static void SetupUnrealEngine(ConsoleApplication framework, SettingsProvider settings)
    {
        Log.WriteLine("Setup Unreal Engine", ILogOutput.LogType.Notice);
        switch (framework.Platform.OperatingSystem)
        {
            case Core.Modules.PlatformModule.PlatformType.Windows:

                // Handle Git Dependencies (only if explicitly requested)
                // In order for Horde to be able to build, the actual dependencies need to be committed to your perforce depo
                if (framework.Arguments.BaseArguments.Contains("git-dependencies"))
                {
                    string gitDependencies = Path.Combine(settings.RootFolder, "Engine", "Binaries", "DotNET", "GitDependencies", "win-x64", "GitDependencies.exe");
                    Log.WriteLine($"Running {gitDependencies} ...");

                    ProcessLogOutput processLogOutput = new(ILogOutput.LogType.ExternalProcess);
                    ProcessUtil.Execute(gitDependencies, settings.RootFolder, "--force", null,
                        processLogOutput.GetAction());
                }

                // Update path for new redist
                string prereqExecutable = Path.Combine(settings.RootFolder, "Engine", "Extras", "Redist", "en-us", "vc_redist.x64.exe");
                Log.WriteLine($"Running {prereqExecutable} ...");
                ProcessUtil.SpawnHidden(prereqExecutable, "/quiet /norestart");

                string versionSelector = Path.Combine(settings.RootFolder, "Engine", "Binaries", "Win64", "UnrealVersionSelector-Win64-Shipping.exe");
                if (File.Exists(versionSelector))
                {
                    Log.WriteLine($"Running {versionSelector} ...");
                    ProcessUtil.SpawnHidden(versionSelector, "/register");
                }
                break;
            case Core.Modules.PlatformModule.PlatformType.macOS:
                // TODO: Implement macOS requirements
                break;
            case Core.Modules.PlatformModule.PlatformType.Linux:
                // TODO: Implement Linux requirements
                break;
        }
    }
    #endregion
}

