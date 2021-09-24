#nullable enable
using System.IO;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("WriteFile")]
    public class WriteFile : IVerb
    {
        [Option('f', "file", Required = false, HelpText = "Path to file to write too.")]
        public string? File { get; set; }

        [Option('c', "content", Required = false, HelpText = "The content of the file to be written")]
        public string? Content { get; set; }

        public bool CanExecute()
        {
            return !string.IsNullOrEmpty(File);
        }

        public bool Execute()
        {
            string folder = Path.GetDirectoryName(File) ?? string.Empty;
            if (folder != string.Empty && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (string.IsNullOrEmpty(File))
            {
                return false;
            }

            // Some single line fixes
            if (!string.IsNullOrEmpty(Content))
            {
                Content = Content.Replace("___SPACE___", " ");
                System.IO.File.WriteAllLines(File, Content.Split("___NEWLINE___"));
            }
            else
            {
                System.IO.File.WriteAllText(File, string.Empty);
            }

            return true;
        }
    }
}