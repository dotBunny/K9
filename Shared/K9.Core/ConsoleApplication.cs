// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9.Core;

public class ConsoleApplication : IDisposable
{
    public static string DateTimeLongFormat = "yyyy-MM-dd HH:mm:ss";
    public static string TimeShortFormat = "HH:mm:ss";
    public static string DateShortFormat = "yyyy-MM-dd";

	private const string k_LogCategory = "CORE";

    // Builtin Modules
    public readonly ArgumentsModule Arguments = new();
    readonly AssemblyModule m_Assembly = new();
	public readonly EnvironmentModule Environment = new();
	public readonly PlatformModule Platform = new();

    // Configuration
    public readonly ProgramProvider ProgramProvider;

    readonly Timer m_RuntimeTimer = new();
    bool m_HasTerminated;
    readonly bool m_ShouldPause;
    readonly bool m_DisplayRuntime;

    public ConsoleApplication(ConsoleApplicationSettings settings, ProgramProvider programProvider)
    {
        // Immediately setup logging
        if (settings.DefaultLogCategory != null)
        {
            Log.DefaultCategory = settings.DefaultLogCategory;
        }
        Log.AddLogOutputs(settings.LogOutputs);

        Arguments.Init(m_Assembly);
        m_Assembly.Init(Arguments);
        Environment.Init(Platform);

        // Should we pause on leaving?
        m_ShouldPause = settings.PauseOnExit;
        if(Arguments.BaseArguments.Contains("no-pause") || Arguments.BaseArguments.Contains("quiet"))
        {
            m_ShouldPause = false;
        }

        if (settings.DisplayHeader)
        {
            Log.WriteLine($"Core Framework @ {GetCoreGitHead()}", ILogOutput.LogType.Notice);
        }

        m_DisplayRuntime = settings.DisplayRuntime;

        if (settings.RequiresElevatedAccess && !ProcessUtil.IsElevated())
        {
            string relaunchTarget = m_Assembly.EntryAssembly != null ? m_Assembly.EntryAssembly.Location : m_Assembly.ExecutingAssembly.Location;
            Log.WriteLine($"Elevation REQUIRED: {relaunchTarget}", k_LogCategory, ILogOutput.LogType.Error);
            if (Arguments.BaseArguments.Contains("elevation-check"))
            {
                Log.WriteLine($"Elevation FAILED: {relaunchTarget}", k_LogCategory, ILogOutput.LogType.Error);
            }
            else
            {
                Log.Shutdown(); // Need to unlock files
                ProcessUtil.Elevate("dotnet", Directory.GetCurrentDirectory(), $"{relaunchTarget} {Arguments} elevation-check", false);
            }
            m_ShouldPause = false;
            Shutdown();
        }

        // Handle Config
        ProgramProvider = programProvider;

        // Help Route
        if (Arguments.BaseArguments.Contains("help"))
        {
            m_DisplayRuntime = false;
            m_ShouldPause = false;
            OutputHelp();
            Shutdown();
        }
        // Bad Args
        if (!ProgramProvider.IsValid(Arguments))
        {
            // Override as we are going straight to help
            m_DisplayRuntime = false;
            m_ShouldPause = false;
            Log.WriteLine("Issue with parsing configuration from arguments, please check the arguments and try again.", k_LogCategory, ILogOutput.LogType.Error);
            OutputHelp();
            Shutdown();
        }
        ProgramProvider.ParseArguments(Arguments);
    }

	public void Shutdown(bool forced = false)
	{
        if (m_HasTerminated) return;
        m_HasTerminated = true;

        if (m_DisplayRuntime)
        {
            Log.WriteLine($"Runtime {m_RuntimeTimer.GetElapsedMilliseconds()}ms", ILogOutput.LogType.Info, k_LogCategory);
        }
        Log.Shutdown();

        // Set our last know code
        System.Environment.ExitCode = Environment.ExitCode;
        if(m_ShouldPause && !forced)
        {
            Console.WriteLine("Press Any Key To Continue ...");
            try
            {
                Console.ReadKey();
            }
            catch(Exception)
            {
                Console.WriteLine("Unable to capture keystroke. Skipping.");
            }
        }

        // Return to the original directory of launch
        if (Environment.OriginalWorkingDirectory != null)
        {
            Directory.SetCurrentDirectory(Environment.OriginalWorkingDirectory);
        }

        System.Environment.Exit(Environment.ExitCode);
	}

    void OutputHelp()
    {
        if (m_Assembly.EntryAssembly != null)
        {
            Log.WriteLine($"# {m_Assembly.EntryAssembly.GetName().Name} #", ILogOutput.LogType.Info);
        }

        // Do we have a description to output?
        if (!string.IsNullOrEmpty(ProgramProvider.GetDescription()))
        {
            Log.WriteLine($"{ProgramProvider.GetDescription()}", ILogOutput.LogType.Info);
        }

        // Output argument help
        KeyValuePair<string, string>[] arguments = ProgramProvider.GetArgumentHelp();
        if (arguments.Length > 0)
        {
            Log.WriteLine("Arguments:", ILogOutput.LogType.Info);
            foreach (KeyValuePair<string, string> argument in arguments)
            {
                Log.WriteLine($"\t{argument.Key}: {argument.Value}", ILogOutput.LogType.Info);
            }
        }
        // Output flag help
        KeyValuePair<string, string>[] flags = ProgramProvider.GetFlagHelp();
        if (flags.Length > 0)
        {
            Log.WriteLine("Flags:", ILogOutput.LogType.Info);
            foreach (KeyValuePair<string, string> flag in flags)
            {
                Log.WriteLine($"\t{flag.Key}: {flag.Value}", ILogOutput.LogType.Info);
            }
        }
    }

	public void ExceptionHandler(Exception e)
	{
		Log.LineFeed();
		Log.WriteLine(e, "EXCEPTION", ILogOutput.LogType.Error);
		Log.LineFeed();

		// Update exit code
		Environment.UpdateExitCode(e.HResult);
	}

    public void Dispose()
    {
        Shutdown();
    }

    public string GetCoreGitHead()
    {
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(m_Assembly.CoreAssembly.Location);
        string[] productVersion = fvi.ProductVersion.Split('+');
        return productVersion.Length == 2 ? productVersion[1] : "Unknown";
    }

    public string GetApplicationGitHead()
    {
        if (m_Assembly.EntryAssembly == null) return "Unknown";
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(m_Assembly.EntryAssembly.Location);
        string[] productVersion = fvi.ProductVersion.Split('+');
        return productVersion.Length == 2 ? productVersion[1] : "Unknown";
    }
}