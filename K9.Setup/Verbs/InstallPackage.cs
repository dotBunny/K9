using System.Net;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("InstallPackage")]
    public class InstallPackage
    {
        [Option('i', "input", Required = true, HelpText = "Path to input/package to use to install.")]
        public string Input { get; set; }
        
        [Option('o', "output", Required = true, HelpText = "Folder to check exists, and if not extract package into.")]
        public string Output { get; set; }
        
        [Option('p', "password", Required = false, HelpText = "Password to use for authentication.")]
        public string Password { get; set; }
        
        [Option('u', "username", Required = false, HelpText = "Username to use for authentication.")]
        public string Username { get; set; }
        
        public bool CanExecute()
        {
            if (System.IO.Directory.Exists(Output)) return false;
            if (string.IsNullOrEmpty(Input) || string.IsNullOrEmpty(Output)) return false;

            return true;
        }

        public bool Execute()
        {
            // Is this SMB
            var upperPackage = Input.ToUpper();
            if (upperPackage.Substring(0, 6) == "SMB://")
            {
                // var connectionString = Input.Substring(6);
                // var connectionSplit = connectionString.Split('/');
                // var serverAddress = new IPAddress()
                //
                // var serverFolder = "":
                // var serverAddress = upperPackage
                // var smb2Client = SMBLibrary.Client.SMB2Client();
            }
            return true;
        }
    }
}