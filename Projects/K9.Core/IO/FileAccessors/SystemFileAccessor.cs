// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

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
        public int GetReadBufferSize()
        {
            return 4096;
        }

        /// <inheritdoc />
        public int GetWriteBufferSize()
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