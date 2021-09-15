using CommandLine;

namespace K9.TeamCity
{
    public class DefaultOptions
    {
        [Option('h', "host", Required = false, HelpText = "TeamCity Host:Port", Default = "dotbunny.dyndns.org:2018")]
        public string Host { get; set; }
        
        [Option('u', "username", Required = false, HelpText = "TeamCity Username")]
        public string Username { get; set; }
        
        [Option('p', "password", Required = false, HelpText = "TeamCity Password")]
        public string Password { get; set; }
        
        [Option('t', "token", Required = false, HelpText = "TeamCity Token")]
        public string Token { get; set; }
    }
}