using CommandLine;

namespace K9.Setup
{
    public class DefaultOptions
    {
        [Option('u', "user", Required = true, HelpText = "The provided username.")]
        public string Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "The provided password.")]
        public string Password { get; set; }
    }
}