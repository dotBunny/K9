﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

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
        public int GetReadBufferSize()
        {
            return (int)SMB2Client.ClientMaxReadSize;
        }

        /// <inheritdoc />
        public int GetWriteBufferSize()
        {
            return (int)SMB2Client.ClientMaxWriteSize;
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
                            (int)_fileStore.MaxReadSize);

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

                            Log.WriteLine(readFileStatus.ToString(), "SMB");
                            _fileStore.CloseFile(fileHandle);
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
            return !ValidConnection() ? null : new SMBWriteStream(_fileStore, _filePath);
        }

        ~SMBFileAccessor()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _fileStore?.Disconnect();

            // Handle Client
            if (_client != null)
            {
                if (_connected)
                {
                    _client.Logoff();
                }
                _connected = false;
                _client.Disconnect();
            }
        }
    }
}