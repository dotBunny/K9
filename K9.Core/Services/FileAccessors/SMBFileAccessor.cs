using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SMBLibrary;
using SMBLibrary.Client;

namespace K9.Services
{
    public class SMBFileAccessor : IFileAccessor
    {
        private SMB2Client _client;
        private ISMBFileStore _fileStore;
        private bool _connected;
        private NTStatus _loginStatus;
        private NTStatus _fileStoreStatus;
        private string _filePath;
        
        public SMBFileAccessor(string address, string username, string password, string share, string filePath)
        {
            _filePath = filePath;

            _client = new SMB2Client();
            _connected = _client.Connect(IPAddress.Parse(address), SMBTransportType.DirectTCPTransport);
            
            if (!_connected) return;
            
            _loginStatus = _client.Login(String.Empty, username, password);
            _fileStore = _client.TreeConnect(share, out _fileStoreStatus);
        }

        ~SMBFileAccessor()
        {
            if(_connected) Cleanup();
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

        /// <inheritdoc />
        public bool ValidConnection()
        {
            return _connected && 
                   _loginStatus == NTStatus.STATUS_SUCCESS && 
                   _fileStoreStatus == NTStatus.STATUS_SUCCESS;
        }

        /// <inheritdoc />
        public bool Get(ref MemoryStream stream)
        {
            if (!ValidConnection()) return false;
            
            
            var fileCreateStatus = _fileStore.CreateFile(out object fileHandle, out var fileStatus, _filePath, 
                AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, 
                ShareAccess.Read, CreateDisposition.FILE_OPEN, 
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (fileCreateStatus == NTStatus.STATUS_SUCCESS && fileStatus == FileStatus.FILE_OPENED)
            {
                var fileInfoStatus = _fileStore.GetFileInformation(out var result, fileHandle,
                    FileInformationClass.FileAllocationInformation);

                if (fileInfoStatus == NTStatus.STATUS_SUCCESS)
                {
                    FileAllocationInformation fileInfo = (FileAllocationInformation)result;
                    byte[] buffer = new byte[fileInfo.AllocationSize];
                }
                else
                {
                    Log.WriteLine($"Unable to query file stats. {fileInfoStatus}");
                }
            }
            else
            {
                Log.WriteLine($"File not found, or unable to open. {fileCreateStatus}|{fileStatus}");
            }

            throw new Exception();
            
            //
            // if (fileCreateStatus == NTStatus.STATUS_SUCCESS && fileStatus == FileStatus.FILE_OPENED)
            // {
            //     long bytesRead = 0;
            //   
            //     while (true)
            //     {
            //         var readFileStatus = _fileStore.ReadFile(out var data, fileHandle, bytesRead, (int)_client.MaxReadSize);
            //         if (readFileStatus != NTStatus.STATUS_SUCCESS && readFileStatus != NTStatus.STATUS_END_OF_FILE)
            //         {
            //             Cleanup();
            //             return false;
            //         }
            //
            //         if (readFileStatus == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
            //         {
            //             break;
            //         }
            //         bytesRead += data.Length;
            //         stream.Write(data, 0, data.Length);
            //     }
            // }
            // _fileStore.CloseFile(fileHandle);
            // Cleanup();
            return true;
        }
    }
}