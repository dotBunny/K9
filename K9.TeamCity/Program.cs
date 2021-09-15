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

            var parser = new Parser(Settings => Settings.CaseInsensitiveEnumValues = true);
            var results = parser.ParseArguments<BuildChangelist>(Core.Arguments)
                .WithParsed<BuildChangelist>(v =>
                {
                    if (v.CanExecute()) v.Execute();
                });
            
            CommandLineUtil.HandleParserResults(results);
        }
    }
}