using System.IO;
using CommandLine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using K9.IO;

namespace K9.Setup.Verbs
{
    [Verb("Extract")]
    public class CopyFile
    {
        [Option('i', "input", Required = true, HelpText = "The full path (uri) to the file to extract.")]
        public string UriString { get; set; }

        [Option('o', "output", Required = true, HelpText = "Folder to extract the aforementioned file too.")]
        public string OutputPath { get; set; }

        [Option('c', "check", Required = false, Default = true,
            HelpText = "Determine if the output folder exists, skipping copy.")]
        public bool CheckExists { get; set; }

        [Option('x', "extract", Required = false, Default = true,
            HelpText = "Attempt to extract archives into output path.")]
        public bool Extract { get; set; }

        public bool CanExecute()
        {
            if (CheckExists)
            {
                if (Directory.Exists(OutputPath))
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(UriString) || string.IsNullOrEmpty(OutputPath))
            {
                return false;
            }

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


            string upperCaseFilePath = UriString.ToUpper();
            IFileAccessor protocolHandler = UriHandler.GetFileAccessor(UriString);
            if (protocolHandler != null)
            {
                Stream stream = protocolHandler.Get();
                if (stream != null)
                {
                    if (Extract && upperCaseFilePath.EndsWith(".ZIP"))
                    {
                        Log.WriteLine("Extracting ZIP ...", Program.Instance.DefaultLogCategory);
                        ZipFile archive = new ZipFile(stream, false);
                        try
                        {
                            foreach (ZipEntry zipEntry in archive)
                            {
                                if (!zipEntry.IsFile)
                                {
                                    continue;
                                }

                                string entryFileName = zipEntry.Name;
                                byte[] buffer = new byte[4096];
                                Stream zipStream = archive.GetInputStream(zipEntry);

                                string fullZipToPath = Path.Combine(OutputPath, entryFileName);
                                string directoryName = Path.GetDirectoryName(fullZipToPath);
                                if (directoryName is { Length: > 0 } && !Directory.Exists(directoryName))
                                {
                                    Directory.CreateDirectory(directoryName);
                                }

                                using FileStream streamWriter = File.Create(fullZipToPath);
                                StreamUtils.Copy(zipStream, streamWriter, buffer);
                            }
                        }
                        finally
                        {
                            archive.IsStreamOwner = true; // Makes close also shut the underlying stream
                            archive.Close(); // Ensure we release resources
                        }
                    }
                    else
                    {
                        if (File.Exists(OutputPath))
                        {
                            using FileStream file = new FileStream(OutputPath, FileMode.Truncate, FileAccess.Write);
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            file.Write(bytes, 0, bytes.Length);
                        }
                        else
                        {
                            using FileStream file = new FileStream(OutputPath, FileMode.Create, FileAccess.Write);
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            file.Write(bytes, 0, bytes.Length);
                        }
                    }

                    stream.Close();
                }
            }

            return true;
        }
    }
}