using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using CommandLine;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace K9.Setup.Verbs
{
    [Verb("Extract")]
    public class Extract
    {
        [Option('f', "file", Required = true, HelpText = "Full path to the file to extract.")]
        public string FilePath { get; set; }
        
        [Option('o', "output", Required = true, HelpText = "Folder to check exists, and if not extract package into.")]
        public string OutputPath { get; set; }

        [Option('c', "check", Required = false,  Default = true, HelpText = "Determine if the extracted folder exists, skipping the extract.")]
        public bool CheckExists { get; set; }
        
        [Option('p', "password", Required = false, HelpText = "Password to use for authentication.")]
        public string Password { get; set; }
        
        [Option('u', "username", Required = false, HelpText = "Username to use for authentication.")]
        public string Username { get; set; }
        
        public bool CanExecute()
        {
            if (System.IO.Directory.Exists(OutputPath)) return false;
            if (string.IsNullOrEmpty(FilePath) || string.IsNullOrEmpty(OutputPath)) return false;

            return true;
        }

        public bool Execute()
        {
            // Check for absolute path to ZIP?
            // Check for SMB ?
            // Is this SMB
            var upperPackage = FilePath.ToUpper();
            if (upperPackage.Substring(0, 6) == "SMB://")
            {
                var connectionString = FilePath.Substring(6);
                var connectionSplit = connectionString.Split('/', 2);

                var stream = ReadFromSMB(connectionSplit[0], Username, Password, connectionSplit[1], connectionSplit[2]);
                var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(OutputPath);
            }
            return true;
        }


        private MemoryStream ReadFromSMB(string address, string username, string password, string share, string file)
        {
            MemoryStream stream = new MemoryStream();
            SMB2Client client = new SMB2Client(); 
            bool isConnected = client.Connect(IPAddress.Parse(address), SMBTransportType.DirectTCPTransport);
            if (isConnected)
            {
                NTStatus status = client.Login(String.Empty, username, password);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    List<string> shares = client.ListShares(out status);
                    if (shares.Contains(share))
                    {
                        var fileStore = client.TreeConnect(share, out var fileStoreStatus);
                        if (fileStoreStatus == NTStatus.STATUS_SUCCESS)
                        {
                            object fileHandle;
                            var fileCreateStatus = fileStore.CreateFile(out fileHandle, out var fileStatus, file, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                            if (fileCreateStatus == NTStatus.STATUS_SUCCESS)
                            {
                                long bytesRead = 0;
                                while (true)
                                {
                                    status = fileStore.ReadFile(out var data, fileHandle, bytesRead, (int)client.MaxReadSize);
                                    if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                                    {
                                        throw new Exception("Failed to read from file");
                                    }

                                    if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
                                    {
                                        break;
                                    }
                                    bytesRead += data.Length;
                                    stream.Write(data, 0, data.Length);
                                }
                            }
                            fileStore.CloseFile(fileHandle);
                            fileStore.Disconnect();
                        }
                    }
                    client.Logoff();
                }
                client.Disconnect();
            }
            return stream;
        }
    }
}