// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;

namespace K9.Process.Verbs;

[Verb("Kill")]
public class Kill : IVerb
{
    [Option('p', "pid", Required = false, HelpText = "PID of process to attempt to kill.")]
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
            Log.WriteLine($"Kill {_singlePID}", Program.Instance.DefaultLogCategory);
            System.Diagnostics.Process singleProcess = System.Diagnostics.Process.GetProcessById(_singlePID);
            singleProcess.Kill(true);
        }
        else
        {
            // Read file
            string[] lines = File.ReadAllLines(PidsPath);
            int lineCount = lines.Length;
            for (int i = 0; i < lineCount; i++)
            {
                string cleaned = lines[i].Trim();
                if (string.IsNullOrEmpty(cleaned))
                {
                    continue;
                }

                if(int.TryParse(cleaned, out int targetPid))
                {
                    try
                    {
                        System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(targetPid);
                        if (!process.HasExited)
                        {
                            Log.WriteLine($"Killing {targetPid}", Program.Instance.DefaultLogCategory);
                            process.Kill(true);
                        }
                    }
                    catch
                    {
                        Log.WriteLine($"{targetPid} was not available.", Program.Instance.DefaultLogCategory);
                    }
                }
            }
        }
        return true;
    }
}