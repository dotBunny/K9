using CommandLine;
using K9.TeamCity.Verbs;
using K9.Utils;

namespace K9.TeamCity
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.TEAMCITY";

        private static void Main(string[] args)
        {
            // Initialize Core
            Instance = new Program();
            Core.Init(Instance);

            Parser parser = new Parser(Settings => Settings.CaseInsensitiveEnumValues = true);

            ParserResult<object> results = parser.ParseArguments<BuildChangelist, SetParameter>(Core.Arguments);

            bool newResult = results.MapResult(
                (BuildChangelist changelist) => changelist.CanExecute() && changelist.Execute(),
                (SetParameter param) => param.CanExecute() && param.Execute(),
                _ => false);

            if (!newResult)
            {
                CommandLineUtil.HandleParserResults(results);
            }
        }
    }
}