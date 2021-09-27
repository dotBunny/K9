using System;
using System.IO;
using System.Net;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace K9.IO.FileAccessors
{
    public class SMBFileAccessor : IFileAccessor
    {
        private readonly SMB2Client _client;
        private readonly string _filePath;
        private readonly ISMBFileStore _fileStore;
        private readonly NTStatus _fileStoreStatus;
        private readonly NTStatus _loginStatus;
        private bool _connected;

        public SMBFileAccessor(string address, string username, string password, string share, string filePath)
        {
            _filePath = filePath;
            _client = new SMB2Client();
            _connected = _client.Connect(IPAddress.Parse(address), SMBTransportType.DirectTCPTransport);

            if (!_connected)
            {
                return;
            }

            _loginStatus = _client.Login(string.Empty, username, password);
            _fileStore = _client.TreeConnect(share, out _fileStoreStatus);
        }

        /// <inheritdoc />
        public uint GetBlockSize()
        {
            return SMB2Client.ClientMaxReadSize;
        }

        /// <inheritdoc />
        public bool ValidConnection()
        {
            return _connected &&
                   _loginStatus == NTStatus.STATUS_SUCCESS &&
                   _fileStoreStatus == NTStatus.STATUS_SUCCESS;
        }

        /// <inheritdoc />
        public Stream GetReader()
        {
            if (!ValidConnection())
            {
                return null;
            }

            NTStatus fileCreateStatus = _fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus,
                _filePath,
                AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal,
                ShareAccess.Read, CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (fileCreateStatus == NTStatus.STATUS_SUCCESS && fileStatus == FileStatus.FILE_OPENED)
            {
                NTStatus fileInfoStatus = _fileStore.GetFileInformation(out FileInformation result, fileHandle,
                    FileInformationClass.FileStandardInformation);

                if (fileInfoStatus == NTStatus.STATUS_SUCCESS)
                {
                    FileStandardInformation fileInfo = (FileStandardInformation)result;
                    CoalesceStream stream = new(fileInfo.EndOfFile);
                    Log.WriteLine($"Opened {_filePath} for read. Expected size {fileInfo.EndOfFile} bytes.",
                        Core.LogCategory);
                    long bytesRead = 0;

                    Timer timer = new();
                    while (bytesRead < fileInfo.EndOfFile)
                    {
                        NTStatus readFileStatus = _fileStore.ReadFile(out byte[] data, fileHandle, bytesRead,
                            (int)_client.MaxReadSize);
                        if (readFileStatus == NTStatus.STATUS_SUCCESS)
                        {
                            if (data == null)
                            {
                                continue;
                            }

                            bytesRead += data.Length;
                            if (data.Length > 0)
                            {
                                stream.Write(data, 0, data.Length);
                            }
                        }
                        else if (readFileStatus == NTStatus.STATUS_END_OF_FILE)
                        {
                            break;
                        }
                        else
                        {
                            stream.Dispose();
                            Cleanup();
                            return null;
                        }
                    }

                    Log.WriteLine(
                        $"Read {bytesRead} bytes in {timer.GetElapsedSeconds()} seconds (∼{timer.TransferRate(bytesRead)}).",
                        Core.LogCategory);
                    _fileStore.CloseFile(fileHandle);
                    Cleanup();
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream;
                }

                Log.WriteLine($"Unable to query file stats. {fileInfoStatus}", Core.LogCategory);
            }
            else
            {
                Log.WriteLine($"File not found, or unable to open. {fileCreateStatus}|{fileStatus}", Core.LogCategory);
            }

            return null;
        }

        /// <inheritdoc />
        public Stream GetWriter()
        {
            if (!ValidConnection())
            {
                return null;
            }

            NTStatus fileCreateStatus = _fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus,
                _filePath,
                AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal,
                ShareAccess.Read, CreateDisposition.FILE_OVERWRITE_IF,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (fileCreateStatus == NTStatus.STATUS_SUCCESS)
            {
                return new SMBWriteStream(_fileStore, fileHandle);
            }

            return null;
        }

        ~SMBFileAccessor()
        {
            if (_connected)
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            if (_fileStoreStatus == NTStatus.STATUS_SUCCESS)
            {
                _fileStore.Disconnect();
            }

            if (_loginStatus == NTStatus.STATUS_SUCCESS)
            {
                _client.Logoff();
            }

            _client.Disconnect();

            _connected = false;
        }

        public class SMBWriteStream : Stream
        {
            private readonly object _fileHandle;
            private readonly ISMBFileStore _fileStore;
            private long _length;

            public SMBWriteStream(ISMBFileStore fileStore, object fileHandle)
            {
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
}