using CommandLine;
using K9.Unity.Verbs;
using K9.Utils;

namespace K9.Unity
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.UNITY";

        // IDE Pattern
        // TestResults -i ../../../K9.Unity.Tests/Content/unityTestResults-Performance.xml -google 
        // TestResults -i ../../../K9.Unity.Tests/Content/unityTestResults-UnitTests.xml
        // VersionControlSettings --folder D:\Workspaces\dotBunny_NightOwl_Main\Projects\NightOwl
        private static void Main(string[] args)
        {
            // Initialize Core
            Instance = new Program();
            Core.Init(Instance);

            Parser parser = new Parser(Settings => Settings.CaseInsensitiveEnumValues = true);

            ParserResult<object> results = parser.ParseArguments<VersionControlSettings, TestResults>(Core.Arguments);

            bool newResult = results.MapResult(
                (VersionControlSettings vcs) => vcs.CanExecute() && vcs.Execute(),
                (TestResults tests) => tests.CanExecute() && tests.Execute(),
                _ => false);

            if (!newResult)
            {
                CommandLineUtil.HandleParserResults(results);
            }
        }
    }
}