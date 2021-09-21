using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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

        public static string AssemblyLocation = "Undefined";
        public static string WorkspaceRoot = "Undefined";
        public static string Changelist = "0";
        public static string Platform = "Win64";

        public static Config Settings;
        public static Services.Perforce.Config P4Config;
        public static List<string> Arguments = new List<string>();
        public static Dictionary<string, string> OverrideArguments = new Dictionary<string, string>();
        public static Dictionary<string, string> Globals = new Dictionary<string, string>();

        public static void Init(IProgram program)
        {
            Log.WriteLine("Running ...", program.DefaultLogCategory);

            // Clean Arguments
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                // Quoted Argument
                if (arg.StartsWith("\"") && arg.EndsWith("\""))
                {
                    Arguments.Add(arg.Trim());
                }
                else if (arg.Contains(" "))
                {
                    var split = arg.Split(" ");
                    foreach (var s in split)
                    {
                        var check = s.Trim();
                        if (!string.IsNullOrEmpty(check)) Arguments.Add(check);
                    }
                }
                else
                {
                    Arguments.Add(arg);
                }
            }

            AssemblyLocation = Assembly.GetEntryAssembly()?.Location;
            WorkspaceRoot = PerforceUtil.GetWorkspaceRoot();

            // Check if first argument is actually passing in a DLL
            var fakePath = System.IO.Path.GetFullPath(Arguments[0]);
            if (File.Exists(fakePath) && fakePath.EndsWith(".dll") && AssemblyLocation == fakePath)
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

            // Handle some possible trickery with our command lines
            OverrideArguments.Clear();
            for (var i = Arguments.Count - 1; i >= 0; i--)
            {
                var arg = Arguments[i];

                // Our parser will only work with arguments that comply with the ---ARG=VALUE format
                if (arg.StartsWith("---"))
                {
                    if (arg.Contains("="))
                    {
                        var split = arg.IndexOf('=');
                        OverrideArguments.Add(arg.Substring(2, split - 2).ToUpper(), arg.Substring(split + 1));
                    }
                    else
                    {
                        OverrideArguments.Add(arg.Substring(2).ToUpper(), "T");
                    }

                    // Take it out of the normal argument list
                    Arguments.RemoveAt(i);
                }
            }

            if (Arguments.Count > 0)
            {
                Log.WriteLine("Arguments:", LogCategory);
                foreach (var s in Arguments) Log.WriteLine($"\t{s}", LogCategory);
            }

            if (OverrideArguments.Count > 0)
            {
                Log.WriteLine("Override Arguments:", LogCategory);
                foreach (var pair in OverrideArguments) Log.WriteLine($"\t{pair.Key}={pair.Value}", LogCategory);
            }


            #region Handle Specific Overrides

            if (OverrideArguments.ContainsKey(ChangelistKey))
            {
                Changelist = OverrideArguments[ChangelistKey];
                Log.WriteLine($"Using manual changelist: {AssemblyLocation}");
            }

            if (OverrideArguments.ContainsKey(PlatformKey))
            {
                Platform = OverrideArguments[PlatformKey];
                Log.WriteLine($"Using manual platform: {Platform}");
            }

            if (OverrideArguments.ContainsKey(AssemblyLocationKey))
            {
                var newPath = Path.GetFullPath(OverrideArguments[AssemblyLocationKey]);
                if (Directory.Exists(newPath))
                {
                    AssemblyLocation = newPath;
                    Log.WriteLine($"Using manual AssemblyLocation: {AssemblyLocation}");
                }
            }

            if (OverrideArguments.ContainsKey(WorkspaceOverrideKey))
            {
                var newPath = Path.GetFullPath(OverrideArguments[WorkspaceOverrideKey]);
                if (Directory.Exists(newPath))
                {
                    WorkspaceRoot = newPath;
                    Log.WriteLine($"Using manual WorkspaceRoot: {AssemblyLocation}");
                }
            }

            if (OverrideArguments.ContainsKey(ConfigLocationKey))
            {
                var newPath = Path.GetFullPath(OverrideArguments[ConfigLocationKey]);
                if (Directory.Exists(newPath))
                {
                    Settings = new Config(Path.Combine(WorkspaceRoot, Config.FileName));
                    Log.WriteLine($"Using manual config: {AssemblyLocation}");
                }
            }

            #endregion

            // Fallback Settings
            if (Settings == null) Settings = new Config(Path.Combine(WorkspaceRoot, Config.FileName));

            // Initialize Workspace
            P4Config = new Services.Perforce.Config(Path.Combine(WorkspaceRoot, Services.Perforce.Config.FileName));
        }
    }
}