// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net;
using System.Threading;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace K9.IO.FileAccessors
{
    public class SMBFileAccessor : IFileAccessor
    {
        private const int ShortCommandDelay = 250;
        private const int LongCommandDelay = 500;
        private readonly SMB2Client _client;
        private readonly string _filePath;
        private readonly ISMBFileStore _fileStore;
        private readonly NTStatus _fileStoreStatus;
        private readonly NTStatus _loginStatus;
        private bool _connected;

        public SMBFileAccessor(string address, string username, string password, string share, string filePath)
        {
            // Initialize our starting states
            _loginStatus = NTStatus.STATUS_PENDING;
            _fileStoreStatus = NTStatus.STATUS_PENDING;
            _filePath = filePath;

            // Try connecting
            _client = new SMB2Client();
            _connected = _client.Connect(IPAddress.Parse(address), SMBTransportType.DirectTCPTransport);

            // Base level fail just cant happen - sorry.
            if (!_connected)
            {
                return;
            }

            // Lets wait a slight bit before we do anything
            Thread.Sleep(ShortCommandDelay);

            // First try on the login
            _loginStatus = _client.Login(string.Empty, username, password);

            // We didnt fully login, this isn't good but we need to figure out if its recoverable.
            if (_loginStatus != NTStatus.STATUS_SUCCESS)
            {
                // Alert
                Log.WriteLine($"Login [{username}]: {_loginStatus}", "SMB", Log.LogType.Info);
                for (int i = 10; i > 0; i--)
                {
                    // Wait a bit
                    Thread.Sleep(ShortCommandDelay);

                    // Retry login
                    _loginStatus = _client.Login(string.Empty, username, password);

                    if (_loginStatus == NTStatus.STATUS_SUCCESS)
                    {
                        break;
                    }
                    Log.WriteLine($"Login [{username}] ({i}): {_loginStatus}", "SMB", Log.LogType.Info);
                }
            }

            // If we haven't logged in were done here.
            if (_loginStatus != NTStatus.STATUS_SUCCESS)
            {
                Log.WriteLine($"Failed to authenticate in time.", "SMB", Log.LogType.Info);
                return;
            }

            // Wait a bit till our next call
            Thread.Sleep(ShortCommandDelay);

            // Best case this works, again first try!
            _fileStore = _client.TreeConnect(share, out _fileStoreStatus);
            if (_fileStoreStatus != NTStatus.STATUS_SUCCESS)
            {
                // Alert
                Log.WriteLine($"File Store [{share}]: {_fileStoreStatus}", "SMB", Log.LogType.Info);
                for (int i = 10; i > 0; i--)
                {
                    // Wait a bit
                    Thread.Sleep(LongCommandDelay);

                    // Retry accessing share
                    _fileStore = _client.TreeConnect(share, out _fileStoreStatus);

                    if (_fileStoreStatus == NTStatus.STATUS_SUCCESS)
                    {
                        break;
                    }
                    Log.WriteLine($"File Store [{share}]: {_fileStoreStatus}", "SMB", Log.LogType.Info);
                }
            }
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
                Log.WriteLine("Invalid Connection Detected.", Core.LogCategory, Log.LogType.Error);
                return null;
            }

            NTStatus fileCreateStatus = _fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus,
                _filePath,
                AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal,
                ShareAccess.Read, CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            Thread.Sleep(ShortCommandDelay);

            if (fileCreateStatus == NTStatus.STATUS_SUCCESS && fileStatus == FileStatus.FILE_OPENED)
            {
                NTStatus fileInfoStatus = _fileStore.GetFileInformation(out FileInformation result, fileHandle,
                    FileInformationClass.FileStandardInformation);

                Thread.Sleep(ShortCommandDelay);

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
                if (_loginStatus == NTStatus.STATUS_SUCCESS)
                {
                    _client.Logoff();
                }

                _connected = false;
                _client.Disconnect();
            }
        }
    }
}