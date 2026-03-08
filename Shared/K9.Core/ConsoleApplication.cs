// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core.Modules;
using K9.Core.Utils;

namespace K9.Core;

public class ConsoleApplication : IDisposable
{
	private const string k_LogCategory = "CORE";

    // Builtin Modules
    public readonly ArgumentsModule Arguments = new();
    readonly AssemblyModule m_Assembly = new();
	public readonly EnvironmentModule Environment = new();
	public readonly PlatformModule Platform = new();

    readonly Timer m_RuntimeTimer = new();
    bool m_HasTerminated;
    readonly bool m_ShouldPause;
    readonly bool m_DisplayRuntime;

	public ConsoleApplication(ConsoleApplicationSettings settings)
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
            Log.WriteLine($"Core Framework v{m_Assembly.CoreAssembly.GetName().Version} @ {GitInfo.Head}", ILogOutput.LogType.Notice);
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
}