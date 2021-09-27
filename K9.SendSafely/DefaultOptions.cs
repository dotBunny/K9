﻿using CommandLine;

namespace K9.SendSafely
{
    public class DefaultOptions
    {
        [Option('u', "access_id", Required = true, HelpText = "Access Key ID")]
        public string AccessKeyID { get; set; }
        [Option('p', "access_pw", Required = true, HelpText = "Access Key Password")]
        public string AccessKeyPW { get; set; }
        [Option('w', "workspace_id", Required = true, HelpText = "Workspace ID")]
        public string WorkspaceID { get; set; }
        [Option('c', "workspace_code", Required = true, HelpText = "Workspace Code")]
        public string WorkspaceCode { get; set; }
        [Option('f', "folder_id", Required = false, HelpText = "Folder ID")]
        public string FolderID { get; set; }
    }
}