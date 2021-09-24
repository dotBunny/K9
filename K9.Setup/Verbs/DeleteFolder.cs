using System.IO;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("DeleteFolder")]
    public class DeleteFolder : IVerb
    {
        [Option('f', "folder", Required = false, HelpText = "Path to folder to remove.")]
        public string Folder { get; set; }

        public bool CanExecute()
        {
            return true;
        }

        public bool Execute()
        {
            if (Directory.Exists(Folder))
            {
                Directory.Delete(Folder, true);
            }

            return true;
        }
    }
}