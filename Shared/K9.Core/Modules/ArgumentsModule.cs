// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace K9.Core.Modules;

public class ArgumentsModule : IModule
{
	private const string k_LogCategory = "ARGS";

	private readonly List<string> m_BaseArguments = [];
    private readonly List<string> m_UpperArguments = [];
	private readonly Dictionary<string, string> m_OverrideArguments = new();

	public ArgumentsModule()
	{
		// Clean Arguments
		string[] args = Environment.GetCommandLineArgs();

		foreach (string arg in args)
		{
			// Quoted Argument that does not need to be
			if (arg.StartsWith("\"") && arg.EndsWith("\"") && !arg.Contains(' '))
			{
				m_BaseArguments.Add(arg[1..^1]);
                m_UpperArguments.Add(m_BaseArguments.Last().ToUpper());
			}
			else
			{
				m_BaseArguments.Add(arg);
                m_UpperArguments.Add(arg.ToUpper());
			}
		}

		// Look for arguments that come in that need to be spliced into one with a quote
		int argCount = m_BaseArguments.Count - 1;
		List<int> removeIndices = new();
        if (argCount < 2)
        {
            return;
        }

        for (int i = 0; i < argCount; i++)
        {
            if (!m_BaseArguments[i].StartsWith("\"") || m_BaseArguments[i].EndsWith("\""))
            {
                continue;
            }

            StringBuilder newArg = new();
            newArg.Append(m_BaseArguments[i]);
            for (int j = i + 1; j < argCount; j++)
            {
                newArg.Append(' ');
                newArg.Append(m_BaseArguments[j]);
                removeIndices.Add(j);
                if (m_BaseArguments[j].StartsWith("\"") || !m_BaseArguments[j].EndsWith("\""))
                {
                    continue;
                }

                m_BaseArguments[i] = newArg.ToString();
                m_UpperArguments[i] = m_BaseArguments[i].ToUpper();
                i = j;
                break;
            }
        }
        // Post remove
        foreach (int i in removeIndices)
        {
            m_BaseArguments.RemoveAt(i);
            m_UpperArguments.RemoveAt(i);
        }
    }

	public void Init(AssemblyModule assemblyModule)
	{
		// Check if the first argument is actually passing in a DLL
		string fakePath = Path.GetFullPath(m_BaseArguments[0]);

		if (File.Exists(fakePath) && fakePath.EndsWith(".dll") && assemblyModule.AssemblyPath == fakePath)
		{
			m_BaseArguments.RemoveAt(0);
		}

		// Handle some possible trickery with our command lines
		m_OverrideArguments.Clear();
		for (int i = m_BaseArguments.Count - 1; i >= 0; i--)
		{
			string arg = m_BaseArguments[i];

			// Our parser will only work with arguments that comply with the ---ARG=VALUE format
			if (arg.StartsWith("---"))
			{
				if (arg.Contains("="))
				{
					int split = arg.IndexOf('=');
					m_OverrideArguments.Add(arg[3..split].ToUpper(), arg[(split + 1)..]);
				}
				else
				{
					m_OverrideArguments.Add(arg[3..].ToUpper(), "T");
				}

				// Take it out of the normal argument list
				m_BaseArguments.RemoveAt(i);
                m_UpperArguments.RemoveAt(i);
			}
		}

		if (m_BaseArguments.Count > 0)
		{
			Log.WriteLine("BaseArguments:", k_LogCategory, ILogOutput.LogType.Info);
			foreach (string s in m_BaseArguments)
			{
				Log.WriteLine($"\t{s}", k_LogCategory, ILogOutput.LogType.Info);
			}
		}

        if (m_OverrideArguments.Count <= 0)
        {
            return;
        }

        Log.WriteLine("Override BaseArguments:", k_LogCategory, ILogOutput.LogType.Info);
        foreach (KeyValuePair<string, string> pair in m_OverrideArguments)
        {
            Log.WriteLine($"\t{pair.Key}={pair.Value}", k_LogCategory, ILogOutput.LogType.Info);
        }
    }


    public bool HasOverrideArgument(string key)
    {
        return m_OverrideArguments.ContainsKey(key);
    }

    public bool HasArguments()
    {
        return m_BaseArguments.Count > 0 || m_OverrideArguments.Count > 0;
    }

    public bool HasBaseArgument(string key)
    {
        return m_UpperArguments.Contains(key.ToUpper());
    }

    public string GetFirstArgument()
    {
        return m_BaseArguments.Count > 0 ? m_BaseArguments[0] : string.Empty;
    }

    public string GetOverrideArgument(string key)
    {
        return m_OverrideArguments.TryGetValue(key.ToUpper(), out string value) ? value : string.Empty;
    }


    public override string ToString()
    {
        StringBuilder builder = new();

        int argCount = m_BaseArguments.Count;
        for(int i = 0; i < argCount; i++)
        {
            builder.Append($"{m_BaseArguments[i]} ");
        }

        foreach(KeyValuePair<string,string> pair in m_OverrideArguments)
        {
            builder.Append($"---{pair.Key}=\"{pair.Value}\" ");
        }

        return builder.ToString().Trim();
    }
}
