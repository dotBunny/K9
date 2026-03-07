// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace K9.Core.Utils;

public class ProcessMonitor
{
    public bool CheckResponding;
    public bool CheckHasExited;

    public const int BadProcessIdentifier = -1;

    private readonly Process m_Process;
    public ProcessMonitor(int pid)
    {
        m_Process = Process.GetProcessById(pid);
    }

    public ProcessMonitor(Process process)
    {
        m_Process = process;
    }

    public bool IsValid()
    {
        if (CheckResponding && !m_Process.Responding)
        {
            return false;
        }

        return !CheckHasExited || !m_Process.HasExited;
    }

    public void Refresh()
    {
        m_Process.Refresh();
    }

    public void Kill()
    {
        m_Process.Kill();
        Thread.Sleep(500);
    }

    // ReSharper disable once InconsistentNaming
    public static int GetPIDFromFile(string path)
    {
        Log.WriteLine($"Attempt to get PID from {path} ...");
        int pid = BadProcessIdentifier;

        // Look for ProcessInfoPath
        if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out pid))
        {
            Log.WriteLine($"PID found: {pid}");
        }
        else
        {
            Log.WriteLine("PID not found.");
        }
        return pid;
    }

    // ReSharper disable once InconsistentNaming
    public static bool IsValidPID(int pid)
    {
        try
        {
            Process.GetProcessById(pid);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static string GetProcessName(int pid)
    {
        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            Log.WriteLine("Unable to get ProcessName for PID.");
        }
        return "";
    }
}