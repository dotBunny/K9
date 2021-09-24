using System.IO;
using IniParser;
using IniParser.Model;

namespace K9
{
    public class Config
    {
        private const string LogCategory = "CONFIG";
        public const string FileName = "K9.ini";
        public readonly IniData Data;

        public Config(string path = FileName)
        {
            if (File.Exists(path))
            {
                Log.WriteLine("Settings found at " + path, LogCategory);
                FileIniDataParser parser = new();
                Data = parser.ReadFile(path);

                Log.WriteLine($"Version: {Data["General"]["Version"]}", LogCategory);
            }
        }

        /*          //data["UI"]["fullscreen"] = "true";
            //parser.WriteFile("Configuration.ini", data);*/
    }
}