// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace K9.IO
{
    public interface IFileAccessor
    {
        public uint GetBlockSize();

        public int GetReadBufferSize();
        public int GetWriteBufferSize();
        public bool ValidConnection();
        public Stream GetReader();
        public Stream GetWriter();
    }
}