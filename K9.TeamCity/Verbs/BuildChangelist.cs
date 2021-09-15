using System.IO;
using CommandLine;
using K9.Utils;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;
using File = System.IO.File;

namespace K9.TeamCity.Verbs
{
    [Verb("BuildChangelist")]
    public class BuildChangelist : DefaultOptions, IVerb
    {
        [Option('b', "build", Required = false, HelpText = "Target Build ID")]
        public string BuildID { get; set; }
        
        [Option('c', "count", Required = false, Default = 0, HelpText = "How many builds back should the history go?")]
        public int History { get; set; }
        
        [Option('f', "full", Required = false, HelpText = "Where to output full processed report.")]
        public string FullPath { get; set; }
        
        [Option('m', "mini", Required = false, HelpText = "Where to output mini processed report.")]
        public string MiniPath { get; set; }
        
        private BuildChangelistMarkdown _markdown;
        
        public bool CanExecute()
        {
            if (!string.IsNullOrEmpty(Token)) return true;
            
            if (string.IsNullOrEmpty(Username)) return false;
            if (string.IsNullOrEmpty(Password)) return false;

            return true;
        }

        public bool Execute()
        {
            var client = new TeamCityClient(Host);
            if (!string.IsNullOrEmpty(Token))
            {
                client.ConnectWithAccessToken(Token);
            }
            else
            {
                client.Connect(Username, Password);
            }


            if (!string.IsNullOrEmpty(BuildID))
            {
                Build targetBuild = client.Builds.ById(BuildID);
                _markdown = new BuildChangelistMarkdown(client, targetBuild, History);
            }

            Log.Write(_markdown.ToString());
            
            if (!string.IsNullOrEmpty(FullPath))
            {
                var outputPath = Path.GetFullPath(FullPath.FixDirectorySeparator());
                outputPath.MakeWritable();
                File.WriteAllText(outputPath, _markdown.GetFullReport());
            }
            
            if (!string.IsNullOrEmpty(MiniPath))
            {
                var outputPath = Path.GetFullPath(MiniPath.FixDirectorySeparator());
                outputPath.MakeWritable();
                File.WriteAllText(outputPath, _markdown.GetMiniReport());
            }
            
            return true;
        }
    }
}