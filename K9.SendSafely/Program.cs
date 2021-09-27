using CommandLine;
using K9.Setup.Verbs;
using K9.Utils;

namespace K9.Setup
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.SENDSAFELY";

        private static void Main(string[] args)
        {
            // Initialize Core
            Instance = new Program();
            Core.Init(Instance);

            Parser parser = new(Settings => Settings.CaseInsensitiveEnumValues = true);

            ParserResult<object> results =
                parser.ParseArguments<Upload, Download>(
                    Core.Arguments);

            bool newResult = results.MapResult(
                (Upload upload) => upload.CanExecute() && upload.Execute(),
                (Download download) => download.CanExecute() && download.Execute(),
                _ => false);

            if (!newResult)
            {
                CommandLineUtil.HandleParserResults(results);
            }
        }
    }
}