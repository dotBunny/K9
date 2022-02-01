// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using K9.IO;
using K9.Services.Utils;
using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("CopyFile")]
    public class CopyFile
    {
        [Option('i', "input", Required = true, HelpText = "The full path (uri) to the file to extract.")]
        public string UriString { get; set; }

        [Option('o', "output", Required = true, HelpText = "Folder to extract the aforementioned file too.")]
        public string OutputPath { get; set; }

        [Option('c', "check", Required = false, Default = false,
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
                Stream inputStream = inputHandler.GetReader();
                if (inputStream == null)
                {
                    Log.WriteLine("No valid reader was found. Check your input, the input may not exist.",
                        Program.Instance.DefaultLogCategory, Log.LogType.Error);
                    Core.UpdateExitCode(-1, true);
                    return false;
                }

                if (Extract && upperCaseFilePath.EndsWith(".ZIP"))
                {
                    Log.WriteLine("Extracting ZIP ...", Program.Instance.DefaultLogCategory);
                    Timer timer = new();
                    ZipFile archive = new(inputStream, false);
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

                            using (FileStream streamWriter = File.Create(fullZipToPath))
                            {
                                StreamUtils.Copy(zipStream, streamWriter, buffer);
                            }
                            SetFileFlags(fullZipToPath, zipEntry);
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

                    long inputStreamLength = inputStream.Length;
                    byte[] bytes = new byte[bufferSize];
                    long writtenLength = 0;
                    Timer timer = new();
                    while (writtenLength < inputStreamLength)
                    {
                        int readAmount = bufferSize;
                        if (writtenLength + bufferSize > inputStreamLength)
                        {
                            readAmount = (int)(inputStreamLength - writtenLength);
                        }

                        inputStream.Read(bytes, 0, readAmount);

                        // Write read data
                        outputFile.Write(bytes, 0, readAmount);

                        // Add to our offset
                        writtenLength += readAmount;
                    }

                    outputFile.Close();
                    Log.WriteLine(
                        $"Wrote {writtenLength} of {inputStreamLength} bytes in {timer.GetElapsedSeconds()} seconds (∼{timer.TransferRate(writtenLength)}).",
                        Program.Instance.DefaultLogCategory);
                }

                inputStream.Close();
            }

            return true;
        }

        /// <summary>
        ///     Set file attributes based on entry value
        /// </summary>
        /// <remarks>
        ///     This only deals with flags that we care about!
        /// </remarks>
        /// <param name="filePath"></param>
        /// <param name="entry"></param>
        private void SetFileFlags(string filePath, ZipEntry entry)
        {


            // Really all we care about is the executable flag
            switch (entry.HostSystem)
            {
                case 3: // unix based
                case 7: // macOS

                    // Only restore on platforms that matter
                    if (entry.ExternalFileAttributes == -2115174400 &&
                        (PlatformUtil.IsLinux() || PlatformUtil.IsMacOS()))
                    {
                        ProcessUtil.SpawnHiddenProcess("chmod", $"+x {filePath}");
                    }
                    break;
            }
        }
    }
}