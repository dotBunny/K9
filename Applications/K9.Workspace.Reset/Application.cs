// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Loggers;
using K9.Services.Perforce;

namespace K9.Workspace.Reset;

internal static class Application
{
    static int s_ProcessCount;

    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {
            DefaultLogCategory = "WORKSPACE.RESET",
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

            // Try to standardize our file/locations, etc.
            SettingsProvider settings = new(workspaceRoot);

            Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "K9.Workspace.Reset"));
            settings.Output();

            ClearProjectPlugins(framework, settings);
            ClearProject(framework, settings);

            if (s_ProcessCount != 0)
            {
                // We did stuff
                return;
            }

            // Nothing to do so tell the user they messed up
            Log.WriteLine("No valid commands found in arguments", ILogOutput.LogType.Warning);
            Log.WriteLine("Valid arguments include 'project' and 'project-plugins'", ILogOutput.LogType.Info);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }

    static void ClearProject(ConsoleApplication framework, SettingsProvider settingsProvider)
    {
        if (!framework.Arguments.BaseArguments.Contains("project"))
        {
            return;
        }
        s_ProcessCount++;
        Log.WriteLine("Clearing Project Artifacts ...");
        string[] projectDirectories = Directory.GetDirectories(settingsProvider.ProjectsFolder);
        int projectCount = projectDirectories.Length;
        for (int i = 0; i < projectCount; i++)
        {
            string intermediateFolder = Path.Combine(projectDirectories[i], "Intermediate");
            if (Path.Exists(intermediateFolder))
            {
                Log.WriteLine($"Removing {intermediateFolder} ...");
                Directory.Delete(intermediateFolder, true);
            }
            string binariesFolder = Path.Combine(projectDirectories[i], "Binaries");
            if (!Path.Exists(binariesFolder))
            {
                continue;
            }

            Log.WriteLine($"Removing {binariesFolder} ...");
            Directory.Delete(binariesFolder, true);
        }
    }
    static void ClearProjectPlugins(ConsoleApplication framework, SettingsProvider settingsProvider)
    {
        if (!framework.Arguments.BaseArguments.Contains("project-plugins"))
        {
            return;
        }
        s_ProcessCount++;
        Log.WriteLine("Clearing Project Plugin Artifacts ...");
        string[] projectDirectories = Directory.GetDirectories(settingsProvider.ProjectsFolder);
        int projectCount = projectDirectories.Length;
        Log.WriteLine($"Found  {projectCount} Projects.");
        for (int i = 0; i < projectCount; i++)
        {
            string projectDirectory = projectDirectories[i];
            string pluginBaseDirectory = Path.Combine(projectDirectory, "Plugins");
            if (!Path.Exists(pluginBaseDirectory))
            {
                continue;
            }

            string[] pluginDefinitions = Directory.GetFiles(pluginBaseDirectory, "*.uplugin", SearchOption.AllDirectories);
            int pluginDefinitionsCount = pluginDefinitions.Length;
            Log.WriteLine($"Found  {pluginDefinitionsCount} plugins in {pluginBaseDirectory}.");
            for (int j = 0; j < pluginDefinitionsCount; j++)
            {
                string? pluginFolder = Path.GetDirectoryName(pluginDefinitions[j]);
                if (pluginFolder == null)
                {
                    continue;
                }

                Log.WriteLine($"Evaluating {pluginFolder} ...");
                string intermediateFolder = Path.Combine(pluginFolder, "Intermediate");
                if (Path.Exists(intermediateFolder))
                {
                    Log.WriteLine($"Removing {intermediateFolder} ...");
                    Directory.Delete(intermediateFolder, true);
                }
                string binariesFolder = Path.Combine(pluginFolder, "Binaries");
                if (!Path.Exists(binariesFolder))
                {
                    continue;
                }

                Log.WriteLine($"Removing {binariesFolder} ...");
                Directory.Delete(binariesFolder, true);
            }
        }
    }
}