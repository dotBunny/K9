using System.IO;
using CommandLine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using K9.IO;
using K9.Services.Utils;

namespace K9.Setup.Verbs
{
    [Verb("CopyFile")]
    public class CopyFile
    {
        [Option('i', "input", Required = true, HelpText = "The full path (uri) to the file to extract.")]
        public string UriString { get; set; }

        [Option('o', "output", Required = true, HelpText = "Folder to extract the aforementioned file too.")]
        public string OutputPath { get; set; }

        [Option('c', "check", Required = false, Default = true,
            HelpText = "Determine if the output folder exists, skipping copy.")]
        public bool CheckExists { get; set; }

        [Option('x', "extract", Required = false, Default = false,
            HelpText = "Attempt to extract archives into output path.")]
        public bool Extract { get; set; }

        public bool CanExecute()
        {
            if (CheckExists)
            {
                if (File.Exists(OutputPath) || Directory.Exists(OutputPath))
                {
                    return true;
                }
            }

            return !string.IsNullOrEmpty(UriString) && !string.IsNullOrEmpty(OutputPath);
        }

        public bool Execute()
        {
            // In the case were doing a quick check to see if the output is already in place.
            if (CheckExists)
            {
                if (File.Exists(OutputPath) || Directory.Exists(OutputPath))
                {
                    Log.WriteLine("Output already found.", Program.Instance.DefaultLogCategory);
                    return true;
                }
            }

            string upperCaseFilePath = UriString.ToUpper();
            IFileAccessor inputHandler = UriHandler.GetFileAccessor(UriString);
            if (inputHandler != null)
            {
                Stream stream = inputHandler.GetReader();
                if (stream == null)
                {
                    Log.WriteLine("No valid reader was found. Check your input, the input may not exist.");
                    return false;
                }

                if (Extract && upperCaseFilePath.EndsWith(".ZIP"))
                {
                    Log.WriteLine("Extracting ZIP ...", Program.Instance.DefaultLogCategory);
                    Timer timer = new();
                    ZipFile archive = new(stream, false);
                    try
                    {
                        foreach (ZipEntry zipEntry in archive)
                        {
                            if (!zipEntry.IsFile)
                            {
                                continue;
                            }

                            string entryFileName = zipEntry.Name;
                            byte[] buffer = new byte[PlatformUtil.GetBlockSize()];
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
                        Log.WriteLine($"Extracted {archive.Count} entries in {timer.GetElapsedSeconds()} seconds.",
                            Program.Instance.DefaultLogCategory);
                    }
                    finally
                    {
                        archive.IsStreamOwner = true; // Makes close also shut the underlying stream
                        archive.Close(); // Ensure we release resources
                    }
                }
                else
                {
                    IFileAccessor outputHandler = UriHandler.GetFileAccessor(OutputPath);

                    int bufferSize = outputHandler.GetWriteBufferSize();
                    using Stream outputFile = outputHandler.GetWriter();

                    long streamLength = stream.Length;
                    byte[] bytes = new byte[bufferSize];
                    long writtenLength = 0;
                    Timer timer = new();
                    while (writtenLength < streamLength)
                    {
                        int readAmount = bufferSize;
                        if (writtenLength + bufferSize > streamLength)
                        {
                            readAmount = (int)(streamLength - writtenLength);
                        }

                        stream.Read(bytes, 0, readAmount);

                        // Write read data
                        outputFile.Write(bytes, 0, readAmount);

                        // Add to our offset
                        writtenLength += readAmount;
                    }

                    outputFile.Close();
                    Log.WriteLine(
                        $"Wrote {writtenLength} of {streamLength} bytes in {timer.GetElapsedSeconds()} seconds (∼{timer.TransferRate(writtenLength)}).",
                        Program.Instance.DefaultLogCategory);
                }

                stream.Close();
            }

            return true;
        }
    }
}