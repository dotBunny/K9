// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace K9.Utils
{
    public static class ProcessUtil
    {
        
        public static void SetupEnvironmentVariables(this Process process)
        {
            process.StartInfo.EnvironmentVariables.Add("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
        }

        public static bool SpawnProcess(string executablePath, string arguments)
        {
            using (Process ChildProcess = new())
            {
                ChildProcess.SetupEnvironmentVariables();
                ChildProcess.StartInfo.FileName = executablePath;
                ChildProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
                ChildProcess.StartInfo.UseShellExecute = false;
                return ChildProcess.Start();
            }
        }

        public static bool SpawnHiddenProcess(string executablePath, string arguments)
        {
            using (Process ChildProcess = new())
            {
                ChildProcess.SetupEnvironmentVariables();
                ChildProcess.StartInfo.FileName = executablePath;
                ChildProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
                ChildProcess.StartInfo.UseShellExecute = false;
                ChildProcess.StartInfo.RedirectStandardOutput = true;
                ChildProcess.StartInfo.RedirectStandardError = true;
                ChildProcess.StartInfo.CreateNoWindow = true;
                return ChildProcess.Start();
            }
        }

        public static int ExecuteProcess(string executablePath, string workingDirectory, string arguments, string Input,
            TextWriter Log)
        {
            return ExecuteProcess(executablePath, workingDirectory, arguments, Input, Line => Log.WriteLine(Line));
        }

        public static int ExecuteProcess(string executablePath, string workingDirectory, string arguments, string Input,
            out List<string> OutputLines)
        {
            List<string> output = new();
            int returnValue = ExecuteProcess(executablePath, workingDirectory, arguments, Input, Line => output.Add(Line));
            OutputLines = output;
            return returnValue;
        }

        public static int ExecuteProcess(string executablePath, string workingDirectory, string arguments, string Input,
            Action<string> OutputLine)
        {
            using (Process ChildProcess = new())
            {
                object LockObject = new();

                DataReceivedEventHandler OutputHandler = (x, y) =>
                {
                    if (y.Data != null)
                    {
                        lock (LockObject)
                        {
                            OutputLine(y.Data.TrimEnd());
                        }
                    }
                };

                ChildProcess.SetupEnvironmentVariables();
                ChildProcess.StartInfo.WorkingDirectory = workingDirectory;
                ChildProcess.StartInfo.FileName = executablePath;
                ChildProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
                ChildProcess.StartInfo.UseShellExecute = false;
                ChildProcess.StartInfo.RedirectStandardOutput = true;
                ChildProcess.StartInfo.RedirectStandardError = true;
                ChildProcess.OutputDataReceived += OutputHandler;
                ChildProcess.ErrorDataReceived += OutputHandler;
                ChildProcess.StartInfo.RedirectStandardInput = Input != null;
                ChildProcess.StartInfo.CreateNoWindow = true;
                ChildProcess.StartInfo.StandardOutputEncoding = new UTF8Encoding(false, false);
                ChildProcess.Start();
                ChildProcess.BeginOutputReadLine();
                ChildProcess.BeginErrorReadLine();

                if (!string.IsNullOrEmpty(Input))
                {
                    ChildProcess.StandardInput.WriteLine(Input);
                    ChildProcess.StandardInput.Close();
                }

                // Busy wait for the process to exit so we can get a ThreadAbortException if the thread is terminated. It won't wait until we enter managed code
                // again before it throws otherwise.
                for (;;)
                {
                    if (ChildProcess.WaitForExit(20))
                    {
                        ChildProcess.WaitForExit();
                        break;
                    }
                }

                return ChildProcess.ExitCode;
            }
        }
    }
}