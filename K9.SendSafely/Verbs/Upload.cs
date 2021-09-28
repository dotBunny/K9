// Copyright (c) 2018-2021 dotBunny Inc.

using System.IO;
using CommandLine;
using K9.SendSafely;
using SendSafely;
using SendSafely.Objects;
using SSDirectory = SendSafely.Directory;
using File = SendSafely.File;

namespace K9.Setup.Verbs
{
    [Verb("Upload")]
    public class Upload : DefaultOptions
    {
        [Option('s', "source", Required = true, HelpText = "Target File Path")]
        public string SourceFilePath { get; set; }

        [Option('t', "target", Required = false, HelpText = "Override the file name to upload.")]
        public string TargetFileName { get; set; }

        [Option('o', "overwrite", Required = false, Default = true, HelpText = "Should overwrite?")]
        public bool Overwrite { get; set; }

        public bool CanExecute()
        {
            if (System.IO.File.Exists(SourceFilePath))
            {
                return true;
            }

            Log.WriteLine($"Unable to find file at {SourceFilePath}", Program.Instance.DefaultLogCategory);
            return false;
        }

        public bool Execute()
        {
            //Initialize the API
            ClientAPI ssApi = new();
            ssApi.InitialSetup("https://share.unity.com",AccessKeyID, AccessKeyPW);
            string userEmail = ssApi.VerifyCredentials();
            Log.WriteLine("Connected to SendSafely as user " + userEmail, Program.Instance.DefaultLogCategory);

            // Get/set workspace settings
            PackageInformation workspacePackage = ssApi.GetPackageInformation(WorkspaceID);
            workspacePackage.KeyCode = WorkspaceCode;
            Log.WriteLine($"Workspace: {workspacePackage.PackageDescriptor} ({workspacePackage.PackageId})");

            // Query for some file information
            FileInfo fileInfo = new (SourceFilePath);

            // Handle if we want to override the target filename with something other then the source name
            string fileName = !string.IsNullOrEmpty(TargetFileName) ? TargetFileName : Path.GetFileName(SourceFilePath);

            // Find existing file
            string workingDirectoryId = !string.IsNullOrEmpty(FolderID) ? FolderID : workspacePackage.RootDirectoryId;
            SSDirectory directory = ssApi.GetDirectory(workspacePackage.PackageId, workingDirectoryId, 0, 0, "", "");
            FileResponse latestFile = null;
            foreach (FileResponse f in directory.Files)
            {
                if (f.FileName == fileName)
                {
                    latestFile = f;
                    break;
                }
            }

            // Check if we  need to overwrite (aka remove file)
            if (latestFile != null)
            {
                // We dont need to write anything
                if (!Overwrite)
                {
                    Log.WriteLine($"File already exists on SendSafely at this location. If you wish to overwrite this file pass use --overwrite parameter.", Program.Instance.DefaultLogCategory);
                    return true;
                }
                Log.WriteLine($"Removing old file {latestFile.FileName} ({latestFile.FileId}).", Program.Instance.DefaultLogCategory);
                ssApi.DeleteFile(workspacePackage.PackageId, workingDirectoryId, latestFile.FileId);
            }

            // Upload file to location
            Timer timer = new();
            Log.WriteLine($"Starting upload of {fileInfo.Length} bytes to SendSafely.", Program.Instance.DefaultLogCategory);
            ssApi.EncryptAndUploadFileInDirectory(workspacePackage.PackageId, workingDirectoryId, workspacePackage.KeyCode, SourceFilePath, new ProgressCallback());
            Log.WriteLine($"Uploaded finished in {timer.GetElapsedSeconds()} seconds ({timer.TransferRate(fileInfo.Length)}).");
            return true;
        }
    }
}