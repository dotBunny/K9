// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using CommandLine;
using K9.SendSafely;
using SendSafely;
using SendSafely.Objects;
using SSDirectory = SendSafely.Directory;

namespace K9.Setup.Verbs
{
    [Verb("Delete")]
    public class Delete : DefaultOptions
    {
        [Option('t', "target", Required = false, HelpText = "Target file name")]
        public string TargetFileName { get; set; }

        public bool CanExecute()
        {
            return true;
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

            // Find existing file
            string workingDirectoryId = !string.IsNullOrEmpty(FolderID) ? FolderID : workspacePackage.RootDirectoryId;
            SSDirectory directory = ssApi.GetDirectory(workspacePackage.PackageId, workingDirectoryId, 0, 0, "", "");

            if (string.IsNullOrEmpty(TargetFileName))
            {
                foreach (FileResponse f in directory.Files)
                {
                    Log.WriteLine($"Deleting {f.FileName} in {directory.DirectoryName}");
                    ssApi.DeleteFile(workspacePackage.PackageId, workingDirectoryId, f.FileId);
                }
            }
            else
            {
                foreach (FileResponse f in directory.Files)
                {
                    if (f.FileName != TargetFileName)
                    {
                        continue;
                    }

                    Log.WriteLine($"Deleting {f.FileName} in {directory.DirectoryName}");
                    ssApi.DeleteFile(workspacePackage.PackageId, workingDirectoryId, f.FileId);
                    break;
                }
            }


            return true;
        }
    }
}