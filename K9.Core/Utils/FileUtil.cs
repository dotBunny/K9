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

        public static void ForceDeleteFile(this string FilePath)
        {
            if (File.Exists(FilePath))
            {
                File.SetAttributes(FilePath, File.GetAttributes(FilePath) & ~FileAttributes.ReadOnly);
                File.Delete(FilePath);
            }
        }

        public static bool IsSafeToWrite(this string FilePath)
        {
            if (File.Exists(FilePath))
            {
                var attributes = File.GetAttributes(FilePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ||
                    (attributes & FileAttributes.Offline) == FileAttributes.Offline)
                    return false;
            }

            return true;
        }

        public static void MakeWritable(this string absolutePath)
        {
            var fileName = Path.GetFileName(absolutePath);
            var directoryPath = absolutePath.TrimEnd(fileName.ToCharArray());

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            if (File.Exists(absolutePath))
                File.SetAttributes(absolutePath,
                    File.GetAttributes(absolutePath).RemoveAttribute(FileAttributes.ReadOnly));
        }

        private static FileAttributes RemoveAttribute(this FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static string GetPathWithCorrectCase(this FileInfo InInfo)
        {
            var ParentInfo = InInfo.Directory;
            if (InInfo.Exists)
                return Path.Combine(GetPathWithCorrectCase(ParentInfo), ParentInfo.GetFiles(InInfo.Name)[0].Name);
            return Path.Combine(GetPathWithCorrectCase(ParentInfo), InInfo.Name);
        }

        public static string GetPathWithCorrectCase(this DirectoryInfo InInfo)
        {
            var ParentInfo = InInfo.Parent;
            if (ParentInfo == null) return InInfo.FullName.ToUpperInvariant();

            if (InInfo.Exists)
                return Path.Combine(GetPathWithCorrectCase(ParentInfo), ParentInfo.GetDirectories(InInfo.Name)[0].Name);

            return Path.Combine(GetPathWithCorrectCase(ParentInfo), InInfo.Name);
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
    }
}