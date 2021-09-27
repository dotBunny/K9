﻿// Copyright (c) 2018-2021 dotBunny Inc.

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
            return true;
        }

        /// <inheritdoc />
        public Stream GetReader()
        {
            Log.WriteLine($"Open file stream for {_filePath} (R).", Core.LogCategory);
            return File.OpenRead(_filePath);;
        }

        /// <inheritdoc />
        public Stream GetWriter()
        {
            Log.WriteLine($"Open file stream for {_filePath} (W).", Core.LogCategory);
            return File.Create(_filePath);
        }
    }
}