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
        public bool Interactive { get; set; }

        [Option("iusername", Required = false, HelpText = "The user to impersonate (Windows Only).")]
        public string Username { get; set; }
        [Option("ipassword", Required = false, HelpText = "The password of the user to impersonate (Windows Only).")]
        public string Password { get; set; }
        [Option("idomain", Required = false, HelpText = "The domain of the user to impersonate (Windows Only).")]
        public string Domain { get; set; }

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

            int workingArgumentCount = _workingArguments.Count;
            for (int i = workingArgumentCount - 1; i >= 0; i--)
            {
                if (_workingArguments[i] == "--iusername")
                {
                    _workingArguments.RemoveAt(i+1);
                    _workingArguments.RemoveAt(i);
                }
                if (_workingArguments[i] == "--ipassword")
                {
                    _workingArguments.RemoveAt(i+1);
                    _workingArguments.RemoveAt(i);
                }
                if (_workingArguments[i] == "--idomain")
                {
                    _workingArguments.RemoveAt(i+1);
                    _workingArguments.RemoveAt(i);
                }
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
            int exitCode = LaunchUnity(_executablePath, arguments.ToString(), Interactive);
            Core.UpdateExitCode(exitCode);
            return (exitCode == 0);
        }

        public int LaunchUnity(string executable, string arguments, bool interactive, string logFilePath = null, bool shouldCleanupLog = false)
        {
            string trimmedArguments = arguments.Trim();
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.GetTempFileName();
                shouldCleanupLog = true;
            }

            Process process = null;
            if (interactive && PlatformUtil.IsWindows() &&
                !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
#pragma warning disable CA1416
                // Platform.Windows.Impersonate.RunImpersonated(Username, Domain, Password, () => {
                //     Log.WriteLine($"Impersonating {Username} ...", "WRAPPER", Log.LogType.ExternalProcess);
                //     process = StartProcess(executable, trimmedArguments, true, logFilePath);
                // });
                process = StartProcessDifferentSession(Username, Domain, Password, executable, trimmedArguments, true,
                    logFilePath);
#pragma warning restore CA1416
            }
            else
            {
                process = StartProcess(executable, trimmedArguments, interactive, logFilePath);
            }

            if (process == null)
            {
                Log.WriteLine("No process found", "WRAPPER", Log.LogType.Error);
                return -1;
            }

            return WatchProcess(process, logFilePath, shouldCleanupLog);
        }

        private static Process StartProcess(string executable, string arguments, bool interactive, string logFilePath)
        {
            string passthroughArguments = $"{arguments} -logFile {logFilePath}";

            Process process = new();
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.FileName = executable;
            process.StartInfo.WindowStyle = interactive ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.Arguments = passthroughArguments;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = interactive;
            process.StartInfo.Verb = interactive ? "runas" : string.Empty;

            Log.WriteLine($"Launching ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}", "WRAPPER", Log.LogType.ExternalProcess);

            process.Start();
            return process;
        }

        [SupportedOSPlatform("windows")]
        private static Process StartProcessDifferentSession(string username, string domain, string password, string executable, string arguments, bool interactive,
            string logFilePath)
        {
            if (!interactive)
            {
                return StartProcess(executable, arguments, false, logFilePath);
            }
            Log.WriteLine($"Launching on different session ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{executable} {arguments.Trim()}", "WRAPPER", Log.LogType.ExternalProcess);


            uint processId =  Platform.Windows.Impersonate.StartProcessAsUser(username, domain, password, executable, $"{executable} {arguments}", Directory.GetCurrentDirectory(), interactive);

            Process process;
            try
            {
                process = Process.GetProcessById((int)processId);
            }
            catch (Exception e)
            {
                Core.ExceptionHandler(e);
                return null;
            }
            return process;
        }


        private static int WatchProcess(Process process, string logFilePath, bool shouldCleanupLog)
        {
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
                return 0;
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
            return 0;
        }

        private static void CaptureStream( StreamReader logStream )
        {
            string content = logStream.ReadToEnd();
            if ( string.IsNullOrEmpty( content ) ) return;
            Log.Write(content, Log.LogType.ExternalProcess);
        }
    }


}