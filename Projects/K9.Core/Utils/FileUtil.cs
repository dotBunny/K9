// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace K9.Utils
{
    public static class FileUtil
    {
        public static void AlwaysWrite(string outputPath, string contents)
        {
            File.WriteAllText(outputPath, contents);
        }

        public static void ForceDeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
                File.Delete(filePath);
            }
        }

        public static bool IsSafeToWrite(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ||
                    (attributes & FileAttributes.Offline) == FileAttributes.Offline)
                {
                    return false;
                }
            }

            return true;
        }

        public static void MakeWritable(this string absolutePath)
        {
            string fileName = Path.GetFileName(absolutePath);
            if (fileName != null)
            {
                string directoryPath = absolutePath.TrimEnd(fileName.ToCharArray());
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }

            if (File.Exists(absolutePath))
            {
                File.SetAttributes(absolutePath,
                    File.GetAttributes(absolutePath).RemoveAttribute(FileAttributes.ReadOnly));
            }
        }

        private static FileAttributes RemoveAttribute(this FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static string GetPathWithCorrectCase(this FileInfo InInfo)
        {
            DirectoryInfo parentInfo = InInfo.Directory;
            return parentInfo != null
                ? Path.Combine(GetPathWithCorrectCase(parentInfo),
                    InInfo.Exists ? parentInfo.GetFiles(InInfo.Name)[0].Name : InInfo.Name)
                : null;
        }

        public static string GetPathWithCorrectCase(this DirectoryInfo InInfo)
        {
            DirectoryInfo parentInfo = InInfo.Parent;
            return parentInfo == null
                ? InInfo.FullName.ToUpperInvariant()
                : Path.Combine(GetPathWithCorrectCase(parentInfo),
                    InInfo.Exists ? parentInfo.GetDirectories(InInfo.Name)[0].Name : InInfo.Name);
        }

        public static MemoryStream GetMemoryStream(this string FilePath)
        {
            return File.Exists(FilePath) ? new MemoryStream(File.ReadAllBytes(FilePath)) : null;
        }

        public static string FixDirectorySeparator(this string filePath)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return filePath.Replace('\\', Path.DirectorySeparatorChar);
                default:
                    return filePath.Replace('/', Path.DirectorySeparatorChar);
            }
        }

        public static void WriteAllLinesNoExtraLine(string path, params string[] lines)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (lines == null)
                throw new ArgumentNullException("lines");

            using (var stream = File.OpenWrite(path))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                if (lines.Length > 0)
                {
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Write(lines[lines.Length - 1]);
                }
            }
        }
    }
}