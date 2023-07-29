// Copyright (c) 2018-2023 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using K9.Services.Utils;

namespace K9
{
    public class Core
    {
        public const string TimeFormat = "O";
        public const string LogCategory = "K9.CORE";
        public const string ChangelistKey = "CL";
        private const string WorkspaceOverrideKey = "WORKSPACEROOT";
        private const string AssemblyLocationKey = "ASSSEMBLYLOCATION";
        private const string ConfigLocationKey = "K9CONFIG";
        public const string PlatformKey = "PLATFORM";

        public static string AssemblyPath = "Undefined";
        public static string AssemblyLocation = "Undefined";
        public static string WorkspaceRoot = "Undefined";
        public static string Changelist = "0";
        public static string Platform = "Win64";
        public static string AgentName;

        public static int ExitCode = 0;

        public static Config Settings;
        public static Services.Perforce.Config P4Config;
        public static List<string> Arguments = new();
        public static Dictionary<string, string> OverrideArguments = new();
        public static string DefaultLogCategory;

        public static void Init(IProgram program)
        {
            DefaultLogCategory = program.DefaultLogCategory;
            Log.WriteLine("Running ...", program.DefaultLogCategory);

            // Clean Arguments
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                // Quoted Argument that do not need to be
                if (arg.StartsWith("\"") && arg.EndsWith("\"") && !arg.Contains(' '))
                {
                    Arguments.Add(arg.Substring(1, arg.Length - 2));
                }
                else
                {
                    Arguments.Add(arg);
                }
            }

            // Look for arguments that come in that need to be spliced into one with a quote
            int argCount = Arguments.Count - 1;
            List<int> removeIndices = new List<int>();
            if (argCount >= 2)
            {
                for (int i = 0; i < argCount; i++)
                {
                    if (Arguments[i].StartsWith("\"") && !Arguments[i].EndsWith("\""))
                    {
                        StringBuilder newArg = new StringBuilder();
                        newArg.Append(Arguments[i]);
                        for (int j = i + 1; j < argCount; j++)
                        {
                            newArg.Append(' ');
                            newArg.Append(Arguments[j]);
                            removeIndices.Add(j);
                            if (!Arguments[j].StartsWith("\"") && Arguments[j].EndsWith("\""))
                            {
                                Arguments[i] = newArg.ToString();
                                i = j;
                                break;
                            }
                        }
                    }
                }
                // Post remove
                foreach (int i in removeIndices)
                {
                    Arguments.RemoveAt(i);
                }
            }

            AssemblyPath = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(AssemblyPath))
            {
                AssemblyLocation = Path.GetFullPath(Path.Combine(AssemblyLocation, ".."));
            }
            Log.WriteLine($"Assembly Location: {AssemblyLocation}", LogCategory, Log.LogType.Info);

            WorkspaceRoot = PerforceUtil.GetWorkspaceRoot();

            // Check if first argument is actually passing in a DLL
            string fakePath = Path.GetFullPath(Arguments[0]);
            if (File.Exists(fakePath) && fakePath.EndsWith(".dll") && AssemblyPath == fakePath)
            {
                Arguments.RemoveAt(0);
            }

            if (PlatformUtil.IsWindows())
            {
                Platform = Environment.Is64BitOperatingSystem ? "Win64" : "Win32";
            }
            else if (PlatformUtil.IsMacOS())
            {
                Platform = "macOS";
            }
            else
            {
                Platform = "Linux";
            }

            AgentName = Environment.MachineName;

            // Handle some possible trickery with our command lines
            OverrideArguments.Clear();
            for (int i = Arguments.Count - 1; i >= 0; i--)
            {
                string arg = Arguments[i];

                // Our parser will only work with arguments that comply with the ---ARG=VALUE format
                if (arg.StartsWith("---"))
                {
                    if (arg.Contains("="))
                    {
                        int split = arg.IndexOf('=');
                        OverrideArguments.Add(arg.Substring(3, split - 3).ToUpper(), arg.Substring(split + 1));
                    }
                    else
                    {
                        OverrideArguments.Add(arg.Substring(3).ToUpper(), "T");
                    }

                    // Take it out of the normal argument list
                    Arguments.RemoveAt(i);
                }
            }

            if (Arguments.Count > 0)
            {
                Log.WriteLine("Arguments:", LogCategory, Log.LogType.Info);
                foreach (string s in Arguments)
                {
                    Log.WriteLine($"\t{s}", LogCategory, Log.LogType.Info);
                }
            }

            if (OverrideArguments.Count > 0)
            {
                Log.WriteLine("Override Arguments:", LogCategory, Log.LogType.Info);
                foreach (KeyValuePair<string, string> pair in OverrideArguments)
                {
                    Log.WriteLine($"\t{pair.Key}={pair.Value}", LogCategory, Log.LogType.Info);
                }
            }


            #region Handle Specific Overrides

            if (OverrideArguments.ContainsKey(ChangelistKey))
            {
                Changelist = OverrideArguments[ChangelistKey];
                Log.WriteLine($"Using manual changelist: {Changelist}");
            }

            if (OverrideArguments.ContainsKey(PlatformKey))
            {
                Platform = OverrideArguments[PlatformKey];
                Log.WriteLine($"Using manual platform: {Platform}");
            }

            if (OverrideArguments.ContainsKey(AssemblyLocationKey))
            {
                string newPath = Path.GetFullPath(OverrideArguments[AssemblyLocationKey]);
                if (Directory.Exists(newPath))
                {
                    AssemblyLocation = newPath;
                    Log.WriteLine($"Using manual AssemblyLocation: {AssemblyLocation}");
                }
            }

            if (OverrideArguments.ContainsKey(WorkspaceOverrideKey))
            {
                string newPath = Path.GetFullPath(OverrideArguments[WorkspaceOverrideKey]);
                if (Directory.Exists(newPath))
                {
                    WorkspaceRoot = newPath;
                    Log.WriteLine($"Using manual WorkspaceRoot: {WorkspaceRoot}");
                }
            }

            if (OverrideArguments.ContainsKey(ConfigLocationKey))
            {
                string newPath = Path.GetFullPath(OverrideArguments[ConfigLocationKey]);
                if (Directory.Exists(newPath))
                {
                    Settings = new Config(Path.Combine(newPath, Config.FileName));
                    Log.WriteLine($"Using manual config: {Path.Combine(newPath, Config.FileName)}");
                }
            }

            #endregion

            // Fallback Settings
            if (Settings == null)
            {
                Settings = new Config(Path.Combine(WorkspaceRoot, Config.FileName));
            }

            // Initialize Workspace
            P4Config = new Services.Perforce.Config(Path.Combine(WorkspaceRoot, Services.Perforce.Config.FileName));
        }

        public static void Shutdown()
        {
            Console.ResetColor();

            // Set our last know code
            Environment.ExitCode = ExitCode;
        }

        public static void UpdateExitCode(int code, bool forceSet = false)
        {
            if (forceSet)
            {
                ExitCode = code;
                return;
            }

            // This will update the exit code to the last known bad code
            if (code != 0)
            {
                ExitCode = code;
            }
        }

        public static void ExceptionHandler(Exception e)
        {
            Console.WriteLine();
            Console.WriteLine("EXCEPTION");
            Console.WriteLine(e);

            // Update exit code
            UpdateExitCode(e.HResult);
        }

        public static Parser GetDefaultParser(bool ignoreUnknown = false)
        {
            Parser parser = new(config =>
            {
                config.CaseInsensitiveEnumValues = true;
                config.IgnoreUnknownArguments = ignoreUnknown; // Allows for Wrapper to work
            });
            return parser;
        }
    }
}