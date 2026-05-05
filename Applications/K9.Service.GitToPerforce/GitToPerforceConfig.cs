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

    public string? PerforcePort { get; set; } = "ssl:perforce.dotbunny.com:1666";
    public string? PerforceUsername { get; set; }
    public string? PerforcePassword { get; set; }
    public string PerforceClientName { get; set; } = "K9_GitToPerforce_Sync";
    public string PerforceWorkspaceStreamName { get; set; } = "//NEXUS/NEXUS-Sync";
    public string PerforceCommitMessageTemplate { get; set; } = "#K9 Updated Git repository at $GitPath to $GitHash";

    public string GitRepositoryUrl { get; set; } = "https://github.com/dotBunny/NEXUS.git";
    public string GitBranch { get; set; } = "main";
    public string GitRepositoryRelativeRoot { get; set; } = "NEXUS";
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
            Log.WriteLine("A DataRoot is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforcePort))
        {
            Log.WriteLine("PerforcePort username is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceUsername))
        {
            Log.WriteLine("PerforceUsername is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforcePassword))
        {
            Log.WriteLine("PerforcePassword is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceClientName))
        {
            Log.WriteLine("A PerforceClientName is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceWorkspaceStreamName))
        {
            Log.WriteLine("A PerforceWorkspaceStreamName is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }
        if (string.IsNullOrEmpty(PerforceCommitMessageTemplate))
        {
            Log.WriteLine("A PerforceCommitMessageTemplate is REQUIRED", ILogOutput.LogType.Warning);
            return false;
        }

        if (string.IsNullOrEmpty(GitRepositoryUrl))
        {
            Log.WriteLine("A git repository URL is REQUIRED", ILogOutput.LogType.Warning);
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

        config.PerforceClientName = (config.PerforceClientName + "_" + Environment.MachineName.Replace(" ", "_").ToUpper());
    }


}