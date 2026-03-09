// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.KeepAlive;

internal static class Program
{
    static bool s_Alive = true;
    static DateTime s_LastHeartbeat;
    static ProcessMonitor? s_ProcessMonitor;

    static void Main()
    {
        using ConsoleApplication framework = new(
         new ConsoleApplicationSettings
         {
             DefaultLogCategory = "OS.KEEPALIVE",
             LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()],
             PauseOnExit = true,
             RequiresElevatedAccess = false,
         }, new KeepAliveProvider());

        try
        {
            KeepAliveProvider provider = (KeepAliveProvider)framework.ProgramProvider;

            // Setup exit logic
            Log.WriteLine("Press CTRL+C to Exit");
            Console.CancelKeyPress += delegate (object? _, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                s_Alive = false;
            };


            // Get an existing running process, just in case It's still there and this app failed?
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            int pid = ProcessMonitor.GetPIDFromFile(provider.Config.ProcessInfoPath);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            if (pid != ProcessMonitor.BadProcessIdentifier &&
                ProcessMonitor.IsValidPID(pid) &&
                ProcessMonitor.GetProcessName(pid) == Path.GetFileNameWithoutExtension(provider.Config.Application))
            {
                s_ProcessMonitor = new ProcessMonitor(pid)
                {
                    CheckHasExited = provider.Config.CheckHasExited,
                    CheckResponding = provider.Config.CheckResponding,
                };
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
                    StartMonitorProcess(provider.Config);
                }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                s_ProcessMonitor.Refresh();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (!s_ProcessMonitor.IsValid())
                {
                    Log.WriteLine($"Monitor has reported an issue, waiting {provider.Config.TimeoutSleepMilliseconds / 1000} seconds to see if it resolves it self. Last good heartbeat was at {s_LastHeartbeat.ToLongDateString()} on {s_LastHeartbeat.ToLongTimeString()}");
                    Thread.Sleep(provider.Config.TimeoutSleepMilliseconds);

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

                Thread.Sleep(provider.Config.SleepMilliseconds);
            }

            KillMonitorProcess();
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }

        framework.Shutdown();
    }
    static void StartMonitorProcess(KeepAliveConfig config)
    {
        Process startProcess = new();

        startProcess.StartInfo.WorkingDirectory = config.WorkingDirectory;
        startProcess.StartInfo.FileName = config.Application;
        startProcess.StartInfo.Arguments = config.Arguments;
        startProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        startProcess.StartInfo.CreateNoWindow = false;
        startProcess.StartInfo.UseShellExecute = true;

        startProcess.Start();
        Thread.Sleep(config.StartSleepMilliseconds);

        s_ProcessMonitor = new ProcessMonitor(startProcess)
        {
            CheckHasExited = config.CheckHasExited,
            CheckResponding = config.CheckResponding,
        };

        Log.WriteLine($"Started with PID of {startProcess.Id}");
        File.WriteAllText(config.ProcessInfoPath, startProcess.Id.ToString());
    }

    static void KillMonitorProcess()
    {
        if (s_ProcessMonitor == null) return;

        s_ProcessMonitor.Kill();
        s_ProcessMonitor = null;
    }

}
