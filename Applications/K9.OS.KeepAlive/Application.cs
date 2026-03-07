// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using K9.Core;

namespace K9.OS.KeepAlive;

internal static class Application
{
    static bool s_Alive = true;
    static DateTime s_LastHeartbeat;
    static ProcessMonitor? s_ProcessMonitor;
    static KeepAliveConfig? s_Settings;

    static void Main()
    {
        using ConsoleApplication framework = new(
         new ConsoleApplicationSettings()
         {
             DefaultLogCategory = "OS.KEEPALIVE",
             LogOutputs = [new Core.Loggers.ConsoleLogOutput()],
             PauseOnExit = true,
             RequiresElevatedAccess = false,
         });

        try
        {
            // The only argument is the path to a settings file
            if(framework.Arguments.BaseArguments.Count  > 0)
            {
                string arg = framework.Arguments.BaseArguments[0];
                if(File.Exists(arg))
                {
                    s_Settings = KeepAliveConfig.Get(arg);
                }
            }

            // Ok no argument, use default
            s_Settings ??= KeepAliveConfig.Get();

            // Bad settings, let's bounce.
            if (!s_Settings.IsValid())
            {
                framework.Shutdown();
            }

            // Setup exit logic
            Log.WriteLine("Press CTRL+C to Exit");
            Console.CancelKeyPress += delegate (object? _, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                s_Alive = false;
            };


            // Get an existing running process, just in case It's still there and this app failed?
            int pid = ProcessMonitor.GetPIDFromFile(s_Settings.ProcessInfoPath);
            if (pid != ProcessMonitor.BAD_PID &&
                ProcessMonitor.IsValidPID(pid) &&
                ProcessMonitor.GetProcessName(pid) == Path.GetFileNameWithoutExtension(s_Settings.Application))
            {
                s_ProcessMonitor = new ProcessMonitor(pid);
                if (s_ProcessMonitor.IsValid())
                {
                    Log.WriteLine("Found valid process from PID file.");
                }
                else
                {
                    s_ProcessMonitor = null;
                }
            }

            // Main loop
            while (s_Alive)
            {
                if (s_ProcessMonitor == null)
                {
                    StartMonitorProcess();
                }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                s_ProcessMonitor.Refresh();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (!s_ProcessMonitor.IsValid())
                {
                    Log.WriteLine($"Monitor has reported an issue, waiting {s_Settings.TimeoutSleepMilliseconds / 1000} seconds to see if it resolves it self. Last good heartbeat was at {s_LastHeartbeat.ToLongDateString()} on {s_LastHeartbeat.ToLongTimeString()}");
                    Thread.Sleep(s_Settings.TimeoutSleepMilliseconds);

                    if (!s_ProcessMonitor.IsValid())
                    {
                        Log.WriteLine("Restarting!");
                        KillMonitorProcess();
                        continue;
                    }
                }
                else
                {
                    s_LastHeartbeat = DateTime.Now;
                }

                Thread.Sleep(s_Settings.SleepMilliseconds);
            }

            KillMonitorProcess();
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }

        framework.Shutdown();
    }
    static void StartMonitorProcess()
    {
        if (s_Settings == null) return;

        Process startProcess = new();

        startProcess.StartInfo.WorkingDirectory = s_Settings.WorkingDirectory;
        startProcess.StartInfo.FileName = s_Settings.Application;
        startProcess.StartInfo.Arguments = s_Settings.Arguments;
        startProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        startProcess.StartInfo.CreateNoWindow = false;
        startProcess.StartInfo.UseShellExecute = true;

        startProcess.Start();
        Thread.Sleep(s_Settings.StartSleepMilliseconds);

        s_ProcessMonitor = new ProcessMonitor(startProcess);

        Log.WriteLine($"Started with PID of {startProcess.Id}");
        File.WriteAllText(s_Settings.ProcessInfoPath, startProcess.Id.ToString());
    }

    static void KillMonitorProcess()
    {
        if (s_ProcessMonitor == null) return;

        s_ProcessMonitor.Kill();
        s_ProcessMonitor = null;
    }

}
