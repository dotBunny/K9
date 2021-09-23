using System.IO;
using System.IO.Compression;
using CommandLine;

namespace K9.Setup.Verbs
{
    [Verb("Extract")]
    public class Extract
    {
        [Option('i', "input", Required = true, HelpText = "The full path (uri) to the file to extract.")]
        public string UriString { get; set; }
        
        [Option('o', "output", Required = true, HelpText = "Folder to extract the aforementioned file too.")]
        public string OutputPath { get; set; }

        [Option('c', "check", Required = false,  Default = true, HelpText = "Determine if the extracted folder exists, skipping the extract.")]
        public bool CheckExists { get; set; }
        
        public bool CanExecute()
        {
            if (System.IO.Directory.Exists(OutputPath)) return false;
            if (string.IsNullOrEmpty(UriString) || string.IsNullOrEmpty(OutputPath)) return false;

            return true;
        }

        public bool Execute()
        {
            if (CheckExists)
            {
                if (File.Exists(OutputPath) || Directory.Exists(OutputPath))
                {
                    Log.WriteLine("Output already found.", Program.Instance.DefaultLogCategory);
                    return true;
                }
            }

            Log.WriteLine("Extracting ...", Program.Instance.DefaultLogCategory);
            
            var upperCaseFilePath = UriString.ToUpper();
            var protocolHandler = Services.UriHandler.GetFileAccessor(UriString);
            if (protocolHandler != null)
            {
                var stream = new MemoryStream();
                if (protocolHandler.Get(ref stream))
                {
                    if (upperCaseFilePath.EndsWith(".ZIP"))
                    {
                        var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                        if (!Directory.Exists(OutputPath))
                        {
                            Directory.CreateDirectory(OutputPath);
                        }
                        archive.ExtractToDirectory(OutputPath);
                        archive.Dispose();
                    }
                    else
                    {
                        if (File.Exists(OutputPath))
                        {
                            using var file = new FileStream(OutputPath, FileMode.Truncate, FileAccess.Write);
                            var bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            file.Write(bytes, 0, bytes.Length);
                        }
                        else
                        {
                            using var file = new FileStream(OutputPath, FileMode.Create, FileAccess.Write);
                            var bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            file.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
                stream.Close();
                
            }
            return true;
        }
    }
}