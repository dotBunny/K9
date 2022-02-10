// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using K9.SendSafely;
using SendSafely;
using SendSafely.Objects;
using SSDirectory = SendSafely.Directory;
using File = SendSafely.File;

namespace K9.Setup.Verbs
{
    [Verb("Download")]
    public class Download : DefaultOptions
    {
        [Option('s', "source", Required = true, HelpText = "Source file name")]
        public string SourceFile { get; set; }

        [Option('t', "target", Required = true, HelpText = "Target file path")]
        public string FilePath { get; set; }

        [Option('o', "overwrite", Required = false, Default = false, HelpText = "Should overwrite?")]
        public bool Overwrite { get; set; }

        public bool CanExecute()
        {
            return true;
        }

        public bool Execute()
        {
            if (System.IO.File.Exists(FilePath) && !Overwrite)
            {
                Log.WriteLine($"File already exists at {FilePath}. Use --overwrite if necessary.", Program.Instance.DefaultLogCategory);
                return true;
            }

            //Initialize the API
            ClientAPI ssApi = new();
            ssApi.InitialSetup("https://share.unity.com",AccessKeyID, AccessKeyPW);
            string userEmail = ssApi.VerifyCredentials();
            Log.WriteLine("Connected to SendSafely as user " + userEmail, Program.Instance.DefaultLogCategory);

            // Get/set workspace settings
            PackageInformation workspacePackage = ssApi.GetPackageInformation(WorkspaceID);
            workspacePackage.KeyCode = WorkspaceCode;
            Log.WriteLine($"Workspace: {workspacePackage.PackageDescriptor} ({workspacePackage.PackageId})");

            // Find existing file
            string workingDirectoryId = !string.IsNullOrEmpty(FolderID) ? FolderID : workspacePackage.RootDirectoryId;
            SSDirectory directory = ssApi.GetDirectory(workspacePackage.PackageId, workingDirectoryId, 0, 0, "", "");
            FileResponse downloadFile = null;
            foreach (FileResponse f in directory.Files)
            {
                if (f.FileName == SourceFile)
                {
                    downloadFile = f;
                    break;
                }
            }

            if (downloadFile != null)
            {
                FileInfo newFile = ssApi.DownloadFileFromDirectory(workspacePackage.PackageId,
                    workingDirectoryId, downloadFile.FileId, workspacePackage.KeyCode,
                    new ProgressCallback());

                System.IO.File.Move(newFile.FullName, FilePath, true);
            }
            else
            {
                Log.WriteLine($"Unable to find {SourceFile} in Workspace: {workspacePackage.PackageId}/{workingDirectoryId}.");
            }

            return true;
        }
    }
}