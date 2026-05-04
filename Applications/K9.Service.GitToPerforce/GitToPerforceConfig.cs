// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using K9.Core;

namespace K9.Service.GitToPerforce;

public class GitToPerforceConfig
{
    // Required for JSON
    // ReSharper disable PropertyCanBeMadeInitOnly.Global
    public string DataRoot { get; set; } = @"D:\GitToPerforce\";
    public int CheckSleep { get; set; } = 5; //5 * 60;

    public string? PerforceUsername { get; set; }
    public string? PerforcePassword { get; set; }
    public string PerforceRelativeRoot { get; set; } = "workspace";
    public string PerforceWorkspaceName { get; set; } = "K9_GitToPerforce_Sync";
    public string PerforceWorkspaceStreamName { get; set; } = "DETHOL";
    public string PerforceCommitMessageTemplate { get; set; } = "#K9 Updated Git repository at <GitRepositoryRelativeRoot> to <CommitHash>";

    public string GitRepositoryUrl { get; set; } = "https://github.com/dotBunny/NEXUS.git";
    public string GitRemote { get; set; } = "origin/main";
    public string GitBranch { get; set; } = "main";
    public string GitRepositoryRelativeRoot { get; set; } = "Projects/DETHOL/Plugins/NEXUS";
    // ReSharper restore PropertyCanBeMadeInitOnly.Global

    public static GitToPerforceConfig Get(string? jsonPath = null)
    {
        JsonSerializerOptions jsonSettings = new()
        {
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        try
        {
            GitToPerforceConfig? foundConfig;
            if (jsonPath == null)
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sync.json");
                if (!File.Exists(filePath)) return new GitToPerforceConfig();

                string content = File.ReadAllText(filePath);

                foundConfig = JsonSerializer.Deserialize<GitToPerforceConfig>(content, jsonSettings)!;
            }
            else
            {
                if (!File.Exists(jsonPath)) return Get();

                string content = File.ReadAllText(jsonPath);

                foundConfig = JsonSerializer.Deserialize<GitToPerforceConfig>(content, jsonSettings);
                if (foundConfig == null) return Get();
            }

            CleanUpConfig(foundConfig);
            return foundConfig;
        }
        catch (Exception e)
        {
            Log.WriteLine(e.Message, ILogOutput.LogType.Error);
            if(e.StackTrace != null)
            {
                Log.WriteLine(e.StackTrace, ILogOutput.LogType.Error);
            }
            return Get();
        }
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(DataRoot))
        {
            Log.WriteLine("A data root is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceUsername))
        {
            Log.WriteLine("Perforce username is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforcePassword))
        {
            Log.WriteLine("Perforce password is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceWorkspaceName))
        {
            Log.WriteLine("A relative root for where to checkout the Perforce workspace is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceRelativeRoot))
        {
            Log.WriteLine("A relative path to where workspace should go is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceWorkspaceStreamName))
        {
            Log.WriteLine("A workspace stream name is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceCommitMessageTemplate))
        {
            Log.WriteLine("A commit message template is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }

        if (string.IsNullOrEmpty(GitRepositoryUrl))
        {
            Log.WriteLine("A git repository URL is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(GitRemote))
        {
            Log.WriteLine("A git remote is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(GitBranch))
        {
            Log.WriteLine("A git branch is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(GitRepositoryRelativeRoot))
        {
            Log.WriteLine("A relative path to where the git repository should go is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }

        return true;
    }

    public static void CleanUpConfig(GitToPerforceConfig config)
    {
        if (config.CheckSleep <= 0)
        {
            config.CheckSleep = 5 * 60;
        }
    }


}