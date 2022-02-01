// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

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
                    FastZip zipInstance = new FastZip();
                    zipInstance.RestoreAttributesOnExtract = true;
                    zipInstance.ExtractZip(inputStream, OutputPath, FastZip.Overwrite.Always, null, null, null, false, true, true);
                    // Log.WriteLine($"Extracted {archive.Count} entries in {timer.GetElapsedSeconds()} seconds.",
                    //         Program.Instance.DefaultLogCategory);
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
    }
}