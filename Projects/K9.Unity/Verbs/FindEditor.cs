// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using CommandLine;
using K9.Services.Utils;

namespace K9.Unity.Verbs
{
    [Verb("FindEditor")]
    public class FindEditor : IVerb
    {
        [Option('v', "version", Required = false, HelpText = "The specific version to find")]
        public string Version { get; set; }

        [Option('i', "input", Required = false, HelpText = "Path to version file")]
        public string Input { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path to output fullpath")]
        public string Output { get; set; }

        /// <inheritdoc />
        public bool CanExecute()
        {
            return !string.IsNullOrEmpty(Version) || (!string.IsNullOrEmpty(Input) && System.IO.File.Exists(Input));
        }

        private void RecordFindings(string path)
        {
            // Dump to log
            Log.WriteLine($"Found Unity @ {path}", "UNITY", Log.LogType.Info);

            // Write to tmp file
            if (!string.IsNullOrEmpty(Output))
            {
                System.IO.File.WriteAllText(Output, path, Encoding.ASCII);
            }
        }

        /// <inheritdoc />
        public bool Execute()
        {
            if (!string.IsNullOrEmpty(Input) && System.IO.File.Exists(Input))
            {
                Version = System.IO.File.ReadAllText(Input).Trim();
            }

            Log.WriteLine($"Finding Unity {Version} ...", "UNITY", Log.LogType.Info);

            // This is a very specific thing to our hardware, we use this environment variable on build machines
            // to tell where editors are installed.
            string installLocation = Environment.GetEnvironmentVariable("unityEditors");
            string installLaunch = Environment.GetEnvironmentVariable("unityLaunch");
            if (!string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(installLaunch) )
            {
                Log.WriteLine("Found environment variable hint ...", "UNITY", Log.LogType.Info);
                string targetBaseFolder = System.IO.Path.Combine(installLocation, Version);
                if (System.IO.Directory.Exists(targetBaseFolder))
                {
                    string executable = System.IO.Path.Combine(targetBaseFolder, installLaunch);
                    if (System.IO.File.Exists(executable))
                    {
                        RecordFindings(executable);
                        return true;
                    }
                }
            }

            // Default Locations
            string editorsPath = null;
            string launchPath = null;
            if (PlatformUtil.IsWindows())
            {
                editorsPath = "C:\\Program Files\\Unity\\Hub\\Editor";
                launchPath = "Editor\\Unity.exe";
            }
            else if (PlatformUtil.IsMacOS())
            {
                editorsPath = "/Applications/Unity";
                launchPath = "Unity.app";
            }
            else if (PlatformUtil.IsLinux())
            {
                editorsPath = "/opt/unity";
                launchPath = "unity";
            }

            if (editorsPath != null)
            {
                string versionedHubPath = System.IO.Path.Combine(editorsPath, Version);
                if (System.IO.Directory.Exists(versionedHubPath))
                {
                    string versionedHubExectuable = System.IO.Path.Combine(versionedHubPath, launchPath);
                    if (System.IO.File.Exists(versionedHubExectuable))
                    {
                        RecordFindings(versionedHubExectuable);
                        return true;
                    }
                }
            }

            return false;
        }


    }
}