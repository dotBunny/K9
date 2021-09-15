using CommandLine;
using K9.Unreal.Verbs;
using K9.Utils;

namespace K9.Unreal
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.UNREAL";

        private static void Main(string[] args)
        {
            // Initialize Core
            Instance = new Program();
            Core.Init(Instance);

            var parser = new Parser(Settings => Settings.CaseInsensitiveEnumValues = true);

            var results = parser.ParseArguments<UERunTestsJSONToNUnitXML>(Core.Arguments);

            var newResult = results.MapResult(
                (UERunTestsJSONToNUnitXML vcs) => vcs.CanExecute() && vcs.Execute(),
                _ => false);

            if (!newResult)
            {
                CommandLineUtil.HandleParserResults(results);
            }
        }
    }
}