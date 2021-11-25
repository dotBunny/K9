// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using CommandLine;
using K9.Services.Utils;


namespace K9.Unity.Verbs
{
    [Verb("Wrapper")]
    public class Wrapper : IVerb
    {
        private const int sessionID = 9;
        public bool Interactive { get; set; }

        private List<string> _workingArguments;
        private string _executablePath;

        /// <inheritdoc />
        public bool CanExecute()
        {
            _workingArguments = new (Core.Arguments.ToArray());

            // Remove verb
            _workingArguments.RemoveAt(0);

            if (_workingArguments.Contains("--interactive"))
            {
                Interactive = true;
                _workingArguments.Remove("--interactive");
            }
            if (_workingArguments.Contains("-i"))
            {
                Interactive = true;
                _workingArguments.Remove("-i");
            }

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
            int exitCode = WrapUnity(_executablePath, arguments.ToString(), Interactive);
            Core.UpdateExitCode(exitCode);
            return (exitCode == 0);
        }

        public static int WrapUnity(string executable, string arguments, bool interactive, string logFilePath = null, bool shouldCleanupLog = false)
        {
            string trimmedArguments = arguments.Trim();
            return PlatformUtil.IsWindows() ?
#pragma warning disable CA1416
                WrapUnity_Windows(executable, trimmedArguments, interactive, logFilePath, shouldCleanupLog) :
#pragma warning restore CA1416
                WrapUnity_Default(executable, trimmedArguments, interactive, logFilePath, shouldCleanupLog);
        }

        private static int WrapUnity_Default(string executable, string arguments, bool interactive, string logFilePath = null, bool shouldCleanupLog = false)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.GetTempFileName();
                shouldCleanupLog = true;
            }
            string passthroughArguments = $"{arguments} -logFile {logFilePath}";

            Process process = new();
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.FileName = executable;
            process.StartInfo.WindowStyle = interactive ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.Arguments = passthroughArguments;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;

            Log.WriteLine($"Launching in {process.StartInfo.WorkingDirectory} ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}", "WRAPPER", Log.LogType.ExternalProcess);

            process.Start();

            return WatchProcess(process, logFilePath, shouldCleanupLog);
        }

        [SupportedOSPlatform("windows")]
        private static int WrapUnity_Windows(string executable, string arguments, bool interactive,
            string logFilePath = null, bool shouldCleanupLog = false)
        {
            if (!interactive)
            {
                return WrapUnity_Default(executable, arguments, false, logFilePath, shouldCleanupLog);
            }
            Log.WriteLine($"Launching on Session {sessionID} in {Directory.GetCurrentDirectory()} ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{executable} {arguments.Trim()}", "WRAPPER", Log.LogType.ExternalProcess);

            uint processId =  Platform.Win32.StartProcessOnSession(
                sessionID, executable, $"{executable} {arguments}", Directory.GetCurrentDirectory());

            Process process;
            try
            {
                process = Process.GetProcessById((int)processId);
            }
            catch (Exception e)
            {
                Core.ExceptionHandler(e);
                return e.HResult;
            }

            return WatchProcess(process, logFilePath, shouldCleanupLog);
        }

        private static int WatchProcess(Process process, string logFilePath, bool shouldCleanupLog)
        {
            using FileStream stream = File.Open( logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
            using StreamReader reader = new ( stream );
            while ( !process.HasExited )
            {
                StandardOutput( reader );
                System.Threading.Thread.Sleep( 500 );
            }

            System.Threading.Thread.Sleep( 500 );
            StandardOutput( reader );

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

        private static void StandardOutput( StreamReader logStream )
        {
            string content = logStream.ReadToEnd();
            if ( string.IsNullOrEmpty( content ) ) return;
            Log.Write(content, Log.LogType.ExternalProcess);
        }
    }


}