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

            Parser parser = new(Settings => Settings.CaseInsensitiveEnumValues = true);

            ParserResult<UERunTestsJSONToNUnitXML> results =
                parser.ParseArguments<UERunTestsJSONToNUnitXML>(Core.Arguments);

            bool newResult = results.MapResult(
                vcs => vcs.CanExecute() && vcs.Execute(),
                _ => false);

            if (!newResult)
            {
                CommandLineUtil.HandleParserResults(results);
            }
        }
    }
}