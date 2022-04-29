// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CommandLine;

namespace K9.Process.Verbs;

[Verb("Wait")]
public class Wait : IVerb
{
    private const int WaitTime = 10000;

    [Option('p', "pid", Required = false, HelpText = "PID of process to attempt to wait for.")]
    public string Pid { get; set; }
    [Option('f', "file", Required = false, HelpText = "Path to file of PIDs")]
    public string PidsPath { get; set; }

    private int _singlePID = -1;

    /// <inheritdoc />
    public bool CanExecute()
    {
        if (!string.IsNullOrEmpty(PidsPath))
        {
            return File.Exists(PidsPath);
        }
        return int.TryParse(Pid, out _singlePID);
    }

    /// <inheritdoc />
    public bool Execute()
    {
        if (_singlePID != -1)
        {
            System.Diagnostics.Process singleProcess = System.Diagnostics.Process.GetProcessById(_singlePID);
            if (!singleProcess.HasExited)
            {
                Log.WriteLine($"Waiting on {_singlePID} ...", Program.Instance.DefaultLogCategory);
                while (!singleProcess.HasExited)
                {
                    Thread.Sleep(WaitTime);
                }
            }
        }
        else
        {
            // Read file
            string[] lines = File.ReadAllLines(PidsPath);
            int lineCount = lines.Length;
            List<System.Diagnostics.Process> processes = new (lineCount);
            for (int i = 0; i < lineCount; i++)
            {
                string cleaned = lines[i].Trim();
                if (string.IsNullOrEmpty(cleaned))
                {
                    continue;
                }

                if(int.TryParse(cleaned, out int targetPid))
                {

                    System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(targetPid);
                    if (!process.HasExited)
                    {
                        Log.WriteLine($"Found {targetPid}", Program.Instance.DefaultLogCategory);
                        processes.Add(process);
                    }
                }
            }

            while (true)
            {
                int waitCount = processes.Count;
                Log.WriteLine($"Waiting on {waitCount} process ...", Program.Instance.DefaultLogCategory);
                for (int i = waitCount - 1; i >= 0; i--)
                {
                    System.Diagnostics.Process p = processes[i];
                    if (p.HasExited)
                    {
                        processes.RemoveAt(i);
                    }
                }

                if (waitCount <= 0)
                {
                    break;
                }

                Thread.Sleep(WaitTime);
            }
        }
        return true;
    }
}