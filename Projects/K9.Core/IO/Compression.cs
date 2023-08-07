// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using K9.Services.Utils;
using K9.Utils;

namespace K9.IO;

public static class Compression
{
    public enum CompressionLevel : int
    {
        None = 0,
        Lowest = 1,
        Lower = 2,
        Low = 3,
        MidLow = 4,
        Medium = 5,
        MidHigh = 6,
        High = 7,
        Higher = 8,
        Highest = 9
    }
    public static bool AddToZip(string zipFilePath, string[] files, CompressionLevel compression = CompressionLevel.None)
    {
        try
        {
            using ZipOutputStream zipStream = new ZipOutputStream(File.Create(zipFilePath));
            Console.WriteLine($"Creating zip file at {zipFilePath}.");
            zipStream.SetLevel((int)compression);
            int fileCount = files.Length;
            for (int i = 0; i < fileCount; i++)
            {
                string filePath = files[i];
                FileInfo info = new FileInfo(filePath);
                Console.WriteLine($"Adding {info.Name} from {filePath}.");
                ZipEntry entry = new ZipEntry(info.Name) { DateTime = info.LastWriteTime, Size = info.Length };
                zipStream.PutNextEntry(entry);
                byte[] buffer = new byte[4096];
                int byteCount = 0;
                using FileStream inputStream = File.OpenRead(filePath);
                byteCount = inputStream.Read(buffer, 0, buffer.Length);
                while (byteCount > 0)
                {
                    zipStream.Write(buffer, 0, byteCount);
                    byteCount = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ExtractZip(string zipFilePath, string outputPath)
    {
        if (File.Exists(zipFilePath))
        {
            using FileStream stream = new (zipFilePath, FileMode.Open);
            ExtractStream(stream, outputPath);
        }
    }

    public static void ExtractStream(Stream inputStream, string outputPath)
    {
        if (PlatformUtil.IsMacOS() || PlatformUtil.IsLinux())
        {
            string tempFile = Path.GetTempFileName();
            FileUtil.WriteStream(inputStream, tempFile);
            Directory.CreateDirectory(outputPath);
            Timer timer = new();
            ProcessUtil.ExecuteProcess("unzip", outputPath, $"{tempFile} -d {outputPath}", null, (processID,s) =>
            {
                Console.WriteLine(s);
            });
            Log.WriteLine($"Extracted archive in {timer.GetElapsedSeconds()} seconds.",
                "COMPRESSION");
        }
        else
        {
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

                    string fullZipToPath = Path.Combine(outputPath, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName is { Length: > 0 } && !Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
                Log.WriteLine($"Extracted {archive.Count} entries in {timer.GetElapsedSeconds()} seconds.", "COMPRESSION");
            }
            finally
            {
                archive.IsStreamOwner = true; // Makes close also shut the underlying stream
                archive.Close(); // Ensure we release resources
            }
        }
    }
}