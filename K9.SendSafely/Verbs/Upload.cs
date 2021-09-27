// Copyright (c) 2018-2021 dotBunny Inc.

using CommandLine;
using K9.SendSafely;
using SendSafely;

namespace K9.Setup.Verbs
{
    public class Upload : DefaultOptions
    {
        [Option('o', "overrite", Required = false, Default = true, HelpText = "Should overrite?")]
        public bool Overrite { get; set; }

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

            return true;
        }
    }
}