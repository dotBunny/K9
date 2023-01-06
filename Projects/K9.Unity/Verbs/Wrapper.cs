// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommandLine;

namespace K9.Unity.Verbs
{
    [Verb("Wrapper")]
    public class Wrapper : IVerb
    {
        private List<string> _workingArguments;
        private string _executablePath;
        private string _logPath;

        /// <inheritdoc />
        public bool CanExecute()
        {
            _workingArguments = new (Core.Arguments.ToArray());

            // Remove verb
            _workingArguments.RemoveAt(0);

            // Get and remove executable from whats being passed through
            _executablePath = _workingArguments[0];
            _workingArguments.RemoveAt(0);

            if (Core.OverrideArguments.ContainsKey("LOG"))
            {
                _logPath = Core.OverrideArguments["LOG"];
            }

            bool foundExecutable = File.Exists(_executablePath);
            if (foundExecutable)
            {
                return true;
            }

            Log.WriteLine($"Unable to find executable @ {_executablePath}.", "WRAPPER", Log.LogType.Error);
            Core.UpdateExitCode(-1, true);
            return false;
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
            int exitCode = LaunchUnity(_executablePath, arguments.ToString(), _logPath);
            Core.UpdateExitCode(exitCode);
            return (exitCode == 0);
        }

        private int LaunchUnity(string executable, string arguments, string logFilePath = null, bool shouldCleanupLog = false)
        {
            string trimmedArguments = arguments.Trim();
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.GetTempFileName();
                shouldCleanupLog = true;
            }
            else if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            Process process = StartProcess(executable, trimmedArguments, logFilePath);
            Console.WriteLine($"##teamcity[setParameter name='PID' value='{process.Id}']");

            // Small time to figure out log file
            // Try to delete file, protecting against longer then expected locks
            bool logFileCreated = false;
            for (int i=0; i< 5; i++ )
            {
                if (File.Exists(logFilePath))
                {
                    Log.WriteLine($"Log file found.", "WRAPPER", Log.LogType.Notice);
                    logFileCreated = true;
                    break;
                }
                Log.WriteLine($"Waiting on log file ...", "WRAPPER", Log.LogType.Notice);
                System.Threading.Thread.Sleep( 1000 );
            }

            // We have a log we have stuff to do
            if (logFileCreated)
            {
                using FileStream stream = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(stream);
                while (!process.HasExited)
                {
                    CaptureStream(reader);
                    System.Threading.Thread.Sleep(500);
                }

                System.Threading.Thread.Sleep(500);
                CaptureStream(reader);

                // We dont need it any
                reader.Close();
                stream.Close();

                // Early out on the return
                if (!shouldCleanupLog || !File.Exists(logFilePath))
                {
                    return process.ExitCode;
                }

                // Try to delete file, protecting against longer then expected locks
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(logFilePath);
                        Log.WriteLine($"Log file deleted.", "WRAPPER", Log.LogType.Notice);
                        break;
                    }
                    catch (Exception)
                    {
                        Log.WriteLine($"Unable to delete {logFilePath} ({i}).", "WRAPPER", Log.LogType.Notice);
                        System.Threading.Thread.Sleep(1000);
                    }
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