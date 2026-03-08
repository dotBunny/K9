// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.LogOutputs;
using K9.Core.Utils;
using K9.Services.Perforce;
using static K9.CommandMap;

namespace K9;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings
        {
            DefaultLogCategory = "K9",
            LogOutputs = [new ConsoleLogOutput()],
            PauseOnExit = false,
            DisplayHeader = false,
            DisplayRuntime = false
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

            Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "K9"));

            // Try to find the desired execution
            string[] programFolderCommands = Directory.GetFiles(settings.DefaultsFolder, $"*{Commands.Extension}", SearchOption.AllDirectories);
            string[] projectFolderCommands = Directory.GetFiles(settings.UnrealProjectsFolder, $"*{Commands.Extension}", SearchOption.AllDirectories);

            CommandMap map = new();

            // Parse Programs
            int programFolderCommandsCount = programFolderCommands.Length;
            for(int i = 0; i < programFolderCommandsCount; i++)
            {
                Commands? c = Commands.Get(programFolderCommands[i]);
                if(c == null)
                {
                    Log.WriteLine($"Unable to parse {programFolderCommands[i]}.", "JSON", ILogOutput.LogType.Error);
                    continue;
                }

                if(c.Actions.Length == 0)
                {
                    Log.WriteLine($"No actions found in {programFolderCommands[i]}.", "JSON", ILogOutput.LogType.Info);
                    continue;
                }

                map.AddCommands(c);
            }

            // Parse Project
            int projectFolderCommandsCount = projectFolderCommands.Length;
            for (int i = 0; i < projectFolderCommandsCount; i++)
            {
                Commands? c = Commands.Get(projectFolderCommands[i]);
                if (c == null)
                {
                    Log.WriteLine($"Unable to parse {projectFolderCommands[i]}.", "JSON", ILogOutput.LogType.Error);
                    continue;
                }
                else if(c.Actions.Length == 0)
                {
                    Log.WriteLine($"No actions found in {projectFolderCommands[i]}.", "JSON", ILogOutput.LogType.Info);
                    continue;
                }

                map.AddCommands(c);
            }

            if(!map.HasCommands())
            {
                Log.WriteLine($"No actions found.", "JSON", ILogOutput.LogType.Info);
                return;
            }

            if (framework.Arguments.BaseArguments.Contains("help") || framework.Arguments.BaseArguments.Count == 0)
            {
                Log.WriteLine(map.GetOutput(), "K9", ILogOutput.LogType.Info);
            }
            else
            {
                CommandMapAction? action = map.GetAction(framework.Arguments.ToString());
                if (action is { Command: not null })
                {
                    string? arguments = action.Arguments;
                    if (arguments != null)
                    {
                        arguments = settings.ReplaceKeywords(arguments);
                    }

                    string? workingDirectory = action.WorkingDirectory;
                    if(workingDirectory != null)
                    {
                        workingDirectory = settings.ReplaceKeywords(workingDirectory);
                    }


                    // We can't just run batch files they have to be run from a command prompt
                    string command = settings.ReplaceKeywords(action.Command);
                    if(action.Command.EndsWith(".bat"))
                    {
                        arguments = $"/K {command} {arguments}";
                        command = "cmd.exe";
                    }

                    // K9 will exit immediately following this 'start'.
                    ProcessUtil.SpawnWithEnvironment(command, arguments, workingDirectory, GetEnvironmentVariables(settings));
                }
                else
                {
                    Log.WriteLine($"Unable to find valid command for query `{framework.Arguments}`.", "K9", ILogOutput.LogType.Error);
                }
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }

    static Dictionary<string, string> GetEnvironmentVariables(SettingsProvider settings)
    {
        // ReSharper disable StringLiteralTypo
        Dictionary<string, string> returnData = new()
        {
            // Universal flag that this was launched from K9
            ["K9"] = "1",

            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",

            // Our own few things
            ["Workspace"] = settings.RootFolder,
            ["BatchFiles"] = settings.UnrealEngineBuildBatchFilesFolder,
            ["K9Temp"] = settings.TempFile,

            // Some things UE uses
            ["COMPUTERNAME"] = Environment.MachineName
        };
        // ReSharper restore StringLiteralTypo

        // P4 Config
        if (File.Exists(settings.PerforceConfigFile))
        {
            PerforceConfig config = new(settings.PerforceConfigFile);
            returnData["P4CLIENT"] = config.Client;
            returnData["P4PORT"] = config.Port;
        }

        return returnData;
    }
}
