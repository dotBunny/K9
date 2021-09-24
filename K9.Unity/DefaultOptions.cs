using CommandLine;

namespace K9.Unity
{
    public class DefaultOptions
    {
        [Option('f', "folder", Required = false, HelpText = "Target Folder")]
        public string Folder { get; set; }
    }
}