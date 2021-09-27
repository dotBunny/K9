// Copyright (c) 2018-2021 dotBunny Inc.

using System.IO;

namespace K9.IO.FileAccessors
{
    public class SystemFileAccessor : IFileAccessor
    {
        private readonly string _filePath;

        public SystemFileAccessor(string filePath)
        {
            _filePath = filePath;
        }

        /// <inheritdoc />
        public uint GetBlockSize()
        {
            return 4096;
        }

        /// <inheritdoc />
        public bool ValidConnection()
        {
            return File.Exists(_filePath);
        }

        /// <inheritdoc />
        public Stream GetReader()
        {
            return !ValidConnection() ? null : File.OpenRead(_filePath);;
        }

        /// <inheritdoc />
        public Stream GetWriter()
        {
            return !ValidConnection() ? null : File.Create(_filePath);
        }
    }
}