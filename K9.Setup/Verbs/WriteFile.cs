using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("WriteFile")]
    public class WriteFile : IVerb
    {
        [Option('f', "file", Required = false, HelpText = "Path to file to write too.")]
        public string File { get; set; }
        
        [Option('c', "content", Required = false, HelpText = "The content of the file to be written")]
        public string Content { get; set; }

        public bool CanExecute()
        {
            return !string.IsNullOrEmpty(File);
        }

        public bool Execute()
        {
            var folder = System.IO.Path.GetDirectoryName(File);
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }

            // Some single line fixes
            Content = Content.Replace("___SPACE___", " ");
            
            System.IO.File.WriteAllLines(File, Content.Split("___NEWLINE___"));
            return true;
        }
    }
}