// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommandLine;

namespace K9.Process.Verbs;

[Verb("Start")]
public class Start : IVerb
{
    public int Count = 1;
    public string PidFile;

    private List<string> _workingArguments;
    private string _executablePath;
    private string _workingFolder;

    /// <inheritdoc />
    public bool CanExecute()
    {
        _workingArguments = new(Core.Arguments.ToArray());

        // Remove verb
        _workingArguments.RemoveAt(0);

        // Get and remove executable from whats being passed through
        _executablePath = _workingArguments[0];
        _workingArguments.RemoveAt(0);
        _workingFolder = Directory.GetParent(_executablePath)?.FullName;

        if (Core.OverrideArguments.ContainsKey("COUNT"))
        {
            int.TryParse(Core.OverrideArguments["COUNT"], out Count);
        }

        if (Core.OverrideArguments.ContainsKey("PID"))
        {
            PidFile = Core.OverrideArguments["PID"];
        }
        else
        {
            if (_workingFolder != null && Directory.Exists(_workingFolder))
            {
                PidFile = Path.Combine(_workingFolder, "pids.log");
            }
            else
            {
                Log.WriteLine("Working folder not found.", Program.Instance.DefaultLogCategory);
                return false;
            }
        }

        bool hasExecutable = File.Exists(_executablePath);
        Log.WriteLine($"Unable to find executable at {_executablePath}.", Program.Instance.DefaultLogCategory);
        return hasExecutable;
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

        Log.WriteLine($"Starting {Count} Processes ...", Program.Instance.DefaultLogCategory);

        List<int> pids = new List<int>();
        for (int i = 0; i < Count; i++)
        {
            System.Diagnostics.Process process = new();
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.FileName = _executablePath;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.Arguments = arguments.ToString().Replace("___INDEX___", i.ToString());
            process.StartInfo.CreateNoWindow = false;

            Log.WriteLine($"Launching Process #{i} ...", Program.Instance.DefaultLogCategory);
            Log.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
            process.Start();

            int pid = process.Id;
            pids.Add(pid);
            Log.WriteLine($"PID {pid}", Program.Instance.DefaultLogCategory);
        }

        // Write PIDs
        int pidCount = pids.Count;
        List<string> pidsLines = new List<string>();
        for (int i = 0; i < pidCount; i++)
        {
            // we can assume the index is the line in the file
            pidsLines.Add(pids[i].ToString());
        }
        Log.WriteLine($"Writing PID file  to {PidFile} ...", Program.Instance.DefaultLogCategory);
        File.WriteAllLines(PidFile, pidsLines);

        return pidCount == Count;
    }
}