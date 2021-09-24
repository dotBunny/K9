using System.IO;
using IniParser;
using IniParser.Model;

namespace K9.Services.Perforce
{
    public class Config
    {
        public const string DefaultPort = "peter.dotbunny.com:1666";
        public const string FileName = "p4config.txt";
        public const string P4Ignore = "p4ignore.txt";
        public const int MaxParallelConnections = 4;
        private readonly string _client;
        private readonly string _password;
        private readonly string _port = DefaultPort;
        private readonly string _username;

        public Config(string path = FileName)
        {
            if (File.Exists(path))
            {
                Log.WriteLine("P4Config found at " + path, Core.LogCategory);
                FileIniDataParser parser = new();
                IniData data = parser.ReadFile(path);

                // Assign to our data structure
                data.TryGetKey("P4USER", out _username);
                data.TryGetKey("P4PASSWD", out _password);
                data.TryGetKey("P4CLIENT", out _client);
                data.TryGetKey("P4PORT", out _port);

                Output();
            }
        }

        public string Username => _username;

        public string Password => _password;

        public string Port => _port;

        public string Client => _client;

        private void Output()
        {
            if (!string.IsNullOrEmpty(Username))
            {
                Log.WriteLine("\tP4USER: " + Username, Core.LogCategory);
            }

            if (!string.IsNullOrEmpty(Password))
            {
                Log.WriteLine("\tP4PASSWD: <REDACTED>", Core.LogCategory);
            }

            if (!string.IsNullOrEmpty(Port))
            {
                Log.WriteLine("\tP4PORT: " + Port, Core.LogCategory);
            }

            if (!string.IsNullOrEmpty(Client))
            {
                Log.WriteLine("\tP4CLIENT: " + Client, Core.LogCategory);
            }
        }
    }
}