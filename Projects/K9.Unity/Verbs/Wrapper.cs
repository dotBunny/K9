// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommandLine;
using K9.Services.Utils;

namespace K9.Unity.Verbs
{
    [Verb("Wrapper")]
    public class Wrapper : IVerb
    {
        private List<string> _workingArguments;
        private string _executablePath;

        /// <inheritdoc />
        public bool CanExecute()
        {
            _workingArguments = new (Core.Arguments.ToArray());

            // Remove verb
            _workingArguments.RemoveAt(0);

            // Get executable
            _executablePath = _workingArguments[0];
            _workingArguments.RemoveAt(0);

            return File.Exists(_executablePath);
        }

        /// <inheritdoc />
        public bool Execute()
        {
            StringBuilder arguments = new();
            foreach (string argument in _workingArguments)
            {
                if (argument.Contains(' '))
                {
                    arguments.Append('"');
                    arguments.Append(argument);
                    arguments.Append('"');

                }
                else
                {
                    arguments.Append(argument);
                }
                arguments.Append(' ');
            }

            int exitCode = WrapUnity(_executablePath, arguments.ToString());
            Core.UpdateExitCode(exitCode);
            return (exitCode == 0);
        }

        public static int WrapUnity(string executable, string arguments, string logFilePath = null, bool shouldCleanupLog = false)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.GetTempFileName();
                shouldCleanupLog = true;
            }
            string passthroughArguments = $"{arguments.TrimEnd()} -logFile {logFilePath}";

            Process process = new();
            process.StartInfo.FileName = executable;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.Arguments = passthroughArguments;
            process.StartInfo.CreateNoWindow = false;

            if (PlatformUtil.IsWindows())
            {
#pragma warning disable CA1416
                process.StartInfo.LoadUserProfile = true;
#pragma warning restore CA1416
            }

            Log.WriteLine("Launching Unity ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}", "WRAPPER", Log.LogType.ExternalProcess);


            process.Start();

            using FileStream stream = File.Open( logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
            using StreamReader reader = new ( stream );
            while ( !process.HasExited )
            {
                StandardOutput( reader );
                System.Threading.Thread.Sleep( 500 );
            }

            System.Threading.Thread.Sleep( 500 );
            StandardOutput( reader );

            if (shouldCleanupLog)
            {
                for (int i=0; i< 5; i++ )
                {
                    try
                    {
                        File.Delete( logFilePath );
                        break;
                    }
                    catch ( Exception)
                    {
                        Log.WriteLine($"Unable to delete {logFilePath} ({i}).", "WRAPPER", Log.LogType.Notice);
                        System.Threading.Thread.Sleep( 1000 );
                    }
                }
            }

            return process.ExitCode;
        }

        private static void StandardOutput( StreamReader logStream )
        {
            string content = logStream.ReadToEnd();
            if ( string.IsNullOrEmpty( content ) ) return;
            Log.Write(content, Log.LogType.ExternalProcess);
        }
    }
}