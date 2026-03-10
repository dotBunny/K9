// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;
using System.Reflection;

namespace K9.Core.Modules;

public class AssemblyModule : IModule
{
	private const string k_LogCategory = "ASSEMBLY";
	private const string k_AssemblyLocationKey = "ASSSEMBLYLOCATION";

	public readonly Assembly CoreAssembly;
    public readonly Assembly ExecutingAssembly;
    public readonly Assembly? EntryAssembly;
	public readonly string AssemblyPath = "Undefined";
    string m_AssemblyLocation = "Undefined";

	public AssemblyModule()
	{
		CoreAssembly = Assembly.GetAssembly(typeof(ConsoleApplication));
        ExecutingAssembly = Assembly.GetExecutingAssembly();
		EntryAssembly = Assembly.GetEntryAssembly();
		if(EntryAssembly != null)
		{
			AssemblyPath = EntryAssembly.Location;
		}

		if (!string.IsNullOrEmpty(AssemblyPath))
		{
			m_AssemblyLocation = Path.GetFullPath(Path.Combine(m_AssemblyLocation, ".."));
		}

		Log.WriteLine($"Assembly Location: {m_AssemblyLocation}", k_LogCategory, ILogOutput.LogType.Info);
	}


	public void Init(ArgumentsModule argumentsModule)
    {
        if (!argumentsModule.HasOverrideArgument(k_AssemblyLocationKey))
        {
            return;
        }

        string newPath = Path.GetFullPath(argumentsModule.GetOverrideArgument(k_AssemblyLocationKey));
        if (!Directory.Exists(newPath))
        {
            return;
        }

        m_AssemblyLocation = newPath;
        Log.WriteLine($"Using manual AssemblyLocation: {m_AssemblyLocation}");
    }
}
