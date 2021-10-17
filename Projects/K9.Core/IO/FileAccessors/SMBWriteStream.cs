// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = System.IO.FileAttributes;

namespace K9.IO.FileAccessors
{
    public class SMBWriteStream : Stream
    {
        private readonly bool _valid;
        private readonly object _fileHandle;
        private readonly ISMBFileStore _fileStore;
        private long _length;

        public SMBWriteStream(ISMBFileStore fileStore, string filePath)
        {

            NTStatus fileCreateStatus = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus,
                filePath,
                AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal,
                ShareAccess.Read, CreateDisposition.FILE_OVERWRITE_IF,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (fileCreateStatus != NTStatus.STATUS_SUCCESS)
            {
                return;
            }

            _valid = true;
            _fileStore = fileStore;
            _fileHandle = fileHandle;
        }

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => _length;

        /// <inheritdoc />
        public override long Position { get; set; }

        /// <inheritdoc />
        public override void Close()
        {
            if (!_valid) return;
            if (_fileStore != null && _fileHandle != null)
            {
                _fileStore.CloseFile(_fileHandle);
            }

            base.Close();
        }

        /// <inheritdoc />
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_valid) return;

            if (count > _fileStore.MaxWriteSize)
            {
                throw new Exception($"Maximum write size exceeded. SMB server reports a maximum write size of {_fileStore.MaxWriteSize}.");
            }

            // Because the write buffer could be potentially bigger then whats actually going to be written
            // we need to check and trim down in that case.
            int written = 0;
            if (buffer.Length > count)
            {
                byte[] finalWrite = new byte[count];
                Array.Copy(buffer, finalWrite, count);
                _fileStore.WriteFile(out written, _fileHandle, offset + Position, finalWrite);
            }
            else
            {
                _fileStore.WriteFile(out written, _fileHandle, offset + Position, buffer);
            }

            Position += written;
            _length += written;
        }
    }
}