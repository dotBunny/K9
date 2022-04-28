﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommandLine;

namespace K9.Setup.Verbs;

[Verb("StartProcess")]
public class StartProcess : IVerb
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
                return false;
            }
        }

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
                arguments.Append(argument.Replace("%i%", i.ToString()));
                arguments.Append('"');
            }
            else
            {
                arguments.Append(argument.Replace("%i%", i.ToString()));
            }

            arguments.Append(' ');
        }

        List<int> pids = new List<int>();
        for (int i = 0; i < Count; i++)
        {
            Process process = new();
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.FileName = _executablePath;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.Arguments = arguments.ToString();
            process.StartInfo.CreateNoWindow = false;

            Log.WriteLine($"Launching Process #{i} ...", "WRAPPER", Log.LogType.ExternalProcess);
            Log.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}", "WRAPPER",
                Log.LogType.ExternalProcess);
            process.Start();

            int pid = process.Id;
            pids.Add(pid);
            Log.WriteLine($"PID {pid}", "WRAPPER", Log.LogType.ExternalProcess);
        }

        // Write PIDs
        int pidCount = pids.Count;
        List<string> pidsLines = new List<string>();
        for (int i = 0; i < pidCount; i++)
        {
            // we can assume the index is the line in the file
            pidsLines.Add(pids[i].ToString());
        }
        File.WriteAllLines(PidFile, pidsLines);

        return pidCount == Count;
    }
}