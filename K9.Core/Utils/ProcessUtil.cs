using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace K9.Utils
{
    public class ProcessUtil
    {
        public static bool SpawnProcess(string FileName, string CommandLine)
        {
            using (Process ChildProcess = new())
            {
                ChildProcess.StartInfo.FileName = FileName;
                ChildProcess.StartInfo.Arguments = string.IsNullOrEmpty(CommandLine) ? "" : CommandLine;
                ChildProcess.StartInfo.UseShellExecute = false;
                return ChildProcess.Start();
            }
        }

        public static bool SpawnHiddenProcess(string FileName, string CommandLine)
        {
            using (Process ChildProcess = new())
            {
                ChildProcess.StartInfo.FileName = FileName;
                ChildProcess.StartInfo.Arguments = string.IsNullOrEmpty(CommandLine) ? "" : CommandLine;
                ChildProcess.StartInfo.UseShellExecute = false;
                ChildProcess.StartInfo.RedirectStandardOutput = true;
                ChildProcess.StartInfo.RedirectStandardError = true;
                ChildProcess.StartInfo.CreateNoWindow = true;
                return ChildProcess.Start();
            }
        }

        public static int ExecuteProcess(string FileName, string WorkingDir, string CommandLine, string Input,
            TextWriter Log)
        {
            return ExecuteProcess(FileName, WorkingDir, CommandLine, Input, Line => Log.WriteLine(Line));
        }

        public static int ExecuteProcess(string FileName, string WorkingDir, string CommandLine, string Input,
            out List<string> OutputLines)
        {
            List<string> output = new();
            int returnValue = ExecuteProcess(FileName, WorkingDir, CommandLine, Input, Line => output.Add(Line));
            OutputLines = output;
            return returnValue;
        }

        public static int ExecuteProcess(string FileName, string WorkingDir, string CommandLine, string Input,
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

                ChildProcess.StartInfo.FileName = FileName;
                ChildProcess.StartInfo.Arguments = string.IsNullOrEmpty(CommandLine) ? "" : CommandLine;
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