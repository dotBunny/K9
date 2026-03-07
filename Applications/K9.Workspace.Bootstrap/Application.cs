// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace K9.Workspace.Bootstrap;

internal static class Application
{
    static bool s_QuietMode;
    static bool s_ShouldClone = true;
    static bool s_ShouldBuild = true;
    static bool s_ShouldSetupWorkspace = true;

    const string k_BuildInfo = "K9_BUILD_SHA";

    static readonly int k_CachedGenerateProjectFilesHash = "GenerateProjectFiles.bat".GetStableUpperCaseHashCode();
    static readonly int k_CachedSetupHash = "Setup.bat".GetStableUpperCaseHashCode();

    static void Main(string[] args)
    {
        try
        {
            Assembly? assembly = Assembly.GetAssembly(typeof(Application));

            if (assembly != null)
                Console.WriteLine($"K9 Bootstrap {assembly.GetName().Version}");

            string? msBuildPath = BootstrapUtils.GetMSBuild();
            if (msBuildPath == null)
            {
                Console.WriteLine("Unable to find an installation of MSBuild.\nYou need to install .NET SDK found at https://dotnet.microsoft.com/en-us/download/dotnet/8.0");
                Environment.ExitCode = 1;
                PressAnyKeyToContinue();
                return;
            }
            Console.WriteLine($"Using MSBuild @ {msBuildPath}");

            // Find the workspace root
            string? workspaceRoot = GetWorkspaceRoot();
            if (workspaceRoot == null)
            {
                Console.WriteLine("Unable to find workspace root.");
                Environment.ExitCode = 2;
                PressAnyKeyToContinue();
                return;
            }
            Console.WriteLine($"Workspace Root @ {workspaceRoot}");

            // Build our source folder path
            string sourceFolder = Path.Combine(workspaceRoot, "K9", "Source");
            Console.WriteLine($"Source Folder @ {sourceFolder}");

            // Grab anything relevant from the command line args
            ParseArguments(args);

            // Run through steps
            CloneSource(sourceFolder);
            BuildSource(sourceFolder);
            WorkspaceSetup(workspaceRoot);
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION!");
            Console.WriteLine(ex);
            Environment.ExitCode = ex.HResult;
        }

        PressAnyKeyToContinue();
    }

    static void ParseArguments(string[] arguments)
    {
        int count = arguments.Length;

        for (int i = 0; i < count; i++)
        {

            if (arguments[i] == "no-clone")
            {
                s_ShouldClone = false;
            }

            if (arguments[i] == "no-build")
            {
                s_ShouldBuild = false;
            }

            if (arguments[i] == "no-workspace")
            {
                s_ShouldSetupWorkspace = false;
            }

            if (arguments[i] == "quiet")
            {
                s_QuietMode = true;
            }
        }

        // Not outputting settings cause we are being quiet
        if (s_QuietMode)
        {
            return;
        }

        Console.WriteLine("Settings");
        Console.WriteLine($"\tClone\t\t{s_ShouldClone}");
        Console.WriteLine($"\tBuild\t\t{s_ShouldBuild}");
        Console.WriteLine($"\tSetup Workspace\t{s_ShouldSetupWorkspace}");
        Console.WriteLine($"\tQuiet Mode\t{s_QuietMode}");
    }
    static void CloneSource(string? sourceFolder)
    {
        if (!s_ShouldClone)
        {
            Console.WriteLine("Skipping Cloning (Argument) ...");
            return;
        }

        // Extra safe
        if (sourceFolder == null)
        {
            Console.WriteLine("Skipping Cloning (Null Source Folder) ...");
            return;
        }

        // To ensure the folder exists, we need to look for the parent
        string? sourceFolderParent = Directory.GetParent(sourceFolder)?.FullName;
        if (!string.IsNullOrEmpty(sourceFolderParent) && !Directory.Exists(sourceFolderParent))
        {
            Directory.CreateDirectory(sourceFolderParent);
        }

#if DEBUG
        Console.WriteLine("Skipping Cloning (Debug Mode) ...");
#else
        // Get or update the source
        if (Directory.Exists(Path.Combine(sourceFolder, ".git")))
        {
            GitUpdateRepo(sourceFolder);
        }
        else
        {
            GitCheckoutRepo("https://github.com/dotBunny/K9.git", sourceFolder);
        }
#endif
    }
    static void BuildSource(string sourceFolder)
    {
        if (!s_ShouldBuild)
        {
            Console.WriteLine("Skipping Building (Argument) ...");
            return;
        }

        string sharedFolder = Path.Combine(sourceFolder, "Shared");

        // Find all projects, exclude Shared and Bootstrap
        string[] projectFiles = Directory.GetFiles(sourceFolder, "*.csproj", SearchOption.AllDirectories);
        int foundCount = projectFiles.Length;
        List<string> parsedFiles = new(foundCount);
        for (int i = 0; i < foundCount; i++)
        {
            // Ignore self
            if (projectFiles[i].EndsWith("K9.Workspace.Bootstrap.csproj")) continue;

            // Might be a bad way to
            if (projectFiles[i].StartsWith(sharedFolder)) continue;

            parsedFiles.Add(projectFiles[i]);
        }

        int compileCount = parsedFiles.Count;
        Console.WriteLine($"Found {compileCount} projects to compile.");

        int exitCode = 0;
        for (int i = 0; i < compileCount; i++)
        {
            Console.WriteLine($"Building {parsedFiles[i]} ...");
            exitCode = BootstrapUtils.ProcessExecute("dotnet", sourceFolder, $"build {parsedFiles[i]} /property:Configuration=Workspace /property:Platform=AnyCPU /t:Rebuild", null, (processIdentifier, line) =>
            {
                Console.WriteLine($"[{processIdentifier}]\t{line}");
            });
        }

        // Check that we had a good build
        if (exitCode == 0)
        {
            File.WriteAllText(Path.Combine(sourceFolder, k_BuildInfo), BootstrapUtils.GitGetLocalCommit(sourceFolder));
        }
    }
    static void WorkspaceSetup(string workspaceRoot)
    {
        if (!s_ShouldSetupWorkspace)
        {
            Console.WriteLine("Skipping Workspace Setup (Argument) ...");
            return;
        }

        // We need to run this process elevated, the main executable is bundled to ensure its elevated, but the library is not.
        string args = $"{Path.Combine(workspaceRoot, "K9", "Binaries", "K9.Workspace.Setup.dll")} no-source no-build";
        if (s_QuietMode)
        {
            args += " quiet";
        }
        BootstrapUtils.ProcessElevate("dotnet", workspaceRoot, args);
    }

    static string? GetWorkspaceRoot(string? workingDirectory = null)
    {
        // If we don't have anything provided, we need to start somewhere.
        if (workingDirectory == null)
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            {
                DirectoryInfo? parentInfo = Directory.GetParent(assembly.Location);
                workingDirectory = parentInfo != null ? parentInfo.FullName : assembly.Location;
            }
            if (workingDirectory == null)
            {
                throw new Exception("Unable to find assembly to determine running location, this is required to find the workspace root.");
            }
        }

        // Check local files for marker
        string[] localFiles = Directory.GetFiles(workingDirectory);
        int localFileCount = localFiles.Length;
        int foundCount = 0;

        // Iterate over the directory files
        for (int i = 0; i < localFileCount; i++)
        {
            int fileNameHash = Path.GetFileName(localFiles[i]).GetStableUpperCaseHashCode();

            if (fileNameHash == k_CachedGenerateProjectFilesHash)
            {
                foundCount++;
            }

            if (fileNameHash == k_CachedSetupHash)
            {
                foundCount++;
            }
        }

        // We know this is the root based on found files
        if (foundCount == 2)
        {
            return workingDirectory;
        }

        // Go back up another directory
        DirectoryInfo? parent = Directory.GetParent(workingDirectory);
        return parent != null ? GetWorkspaceRoot(parent.FullName) : null;
    }
    static void PressAnyKeyToContinue()
    {
        if (s_QuietMode) return;

        Console.WriteLine("Press Any Key To Continue ...");
        try
        {
            Console.ReadKey();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unable to read input. This is usually because this is being ran inside of a P4V shell.");
            Console.WriteLine($"The actual exception is {e.Message}.");
            Console.WriteLine("Feel free to CLOSE this process NOW!");
        }
    }

}
