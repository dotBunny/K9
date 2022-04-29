﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using CommandLine;

namespace K9.Setup.Verbs;

[Verb("WaitProcess")]
public class WaitProcess : IVerb
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
            Process singleProcess = Process.GetProcessById(_singlePID);
            if (!singleProcess.HasExited)
            {
                Log.WriteLine($"Waiting on {_singlePID} ...", "PROCESS", Log.LogType.ExternalProcess);
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
            List<Process> processes = new List<Process>(lineCount);
            for (int i = 0; i < lineCount; i++)
            {
                string cleaned = lines[i].Trim();
                if (string.IsNullOrEmpty(cleaned))
                {
                    continue;
                }

                if(int.TryParse(cleaned, out int targetPid))
                {

                    Process process = Process.GetProcessById(targetPid);
                    if (!process.HasExited)
                    {
                        Log.WriteLine($"Found {targetPid}", "PROCESS", Log.LogType.ExternalProcess);
                        processes.Add(process);
                    }
                }
            }

            while (true)
            {
                int waitCount = processes.Count;
                Log.WriteLine($"Waiting on {waitCount} process ...");
                for (int i = waitCount - 1; i >= 0; i--)
                {
                    Process p = processes[i];
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