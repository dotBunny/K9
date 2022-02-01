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
            _executablePath = _workingArguments[0];
            _workingArguments.RemoveAt(0);

            return File.Exists(_executablePath);
        }

        /// <inheritdoc />
        public bool Execute()
        {
            StringBuilder arguments = new();
            int workingCount = _workingArguments.Count;
            for (int i = 0; i < workingCount; i++)
            {
                string argument = _workingArguments[i];
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
            int exitCode = LaunchUnity(_executablePath, arguments.ToString());
            Core.UpdateExitCode(exitCode);
            return (exitCode == 0);
        }

        public int LaunchUnity(string executable, string arguments, string logFilePath = null, bool shouldCleanupLog = false)
        {
            string trimmedArguments = arguments.Trim();
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.GetTempFileName();
                shouldCleanupLog = true;
            }

            Process process = null;
            // if (PlatformUtil.IsMacOS())
            // {
            //     process = StartProcess("open", $"{executable} --args {trimmedArguments}", logFilePath);
            // }
            // else
            // {
                process = StartProcess(executable, trimmedArguments, logFilePath);
            //}

            if (process == null)
            {
                Log.WriteLine("No process found", "WRAPPER", Log.LogType.Error);
                return -1;
            }

            using FileStream stream = File.Open( logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
            using StreamReader reader = new ( stream );
            while ( !process.HasExited )
            {
                CaptureStream( reader );
                System.Threading.Thread.Sleep( 500 );
            }

            System.Threading.Thread.Sleep( 500 );
            CaptureStream( reader );

            // We dont need it any
            reader.Close();
            stream.Close();

            // Early out on the return
            if (!shouldCleanupLog || !File.Exists(logFilePath))
            {
                return process.ExitCode;
            }

            // Try to delete file, protecting against longer then expected locks
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

            // Finally return code
            return process.ExitCode;
        }

        private static Process StartProcess(string executable, string arguments, string logFilePath)
        {
            string passthroughArguments = $"{arguments} -logFile {logFilePath}";

            Process process = new();
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.FileName = executable;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.Arguments = passthroughArguments;
            process.StartInfo.CreateNoWindow = false;

            Log.WriteLine($"Launching ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}", "WRAPPER", Log.LogType.ExternalProcess);

            process.Start();
            return process;
        }

        private static void CaptureStream( StreamReader logStream )
        {
            string content = logStream.ReadToEnd();
            if ( string.IsNullOrEmpty( content ) ) return;
            Log.Write(content, Log.LogType.ExternalProcess);
        }
    }


}