// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;
using System.Reflection;

namespace K9.Core.Modules
{
	public class AssemblyModule : IModule
	{
		private const string LogCategory = "ASSEMBLY";
		private const string AssemblyLocationKey = "ASSSEMBLYLOCATION";

		public readonly Assembly CoreAssembly;
        public readonly Assembly ExecutingAssembly;
        public readonly Assembly? EntryAssembly;
		public readonly string AssemblyPath = "Undefined";
		public string AssemblyLocation = "Undefined";

		public AssemblyModule()
		{
			CoreAssembly = Assembly.GetAssembly(typeof(K9.Core.ConsoleApplication));
            ExecutingAssembly = Assembly.GetExecutingAssembly();
			EntryAssembly = Assembly.GetEntryAssembly();
			if(EntryAssembly != null)
			{
				AssemblyPath = EntryAssembly.Location;
			}

			if (!string.IsNullOrEmpty(AssemblyPath))
			{
				AssemblyLocation = Path.GetFullPath(Path.Combine(AssemblyLocation, ".."));
			}

			Core.Log.WriteLine($"Assembly Location: {AssemblyLocation}", LogCategory, ILogOutput.LogType.Info);
		}


		public void Init(ArgumentsModule argumentsModule)
		{
			if (argumentsModule.OverrideArguments.ContainsKey(AssemblyLocationKey))
			{
				string newPath = Path.GetFullPath(argumentsModule.OverrideArguments[AssemblyLocationKey]);
				if (Directory.Exists(newPath))
				{
					AssemblyLocation = newPath;
					Core.Log.WriteLine($"Using manual AssemblyLocation: {AssemblyLocation}");
				}
			}
		}
	}
}
