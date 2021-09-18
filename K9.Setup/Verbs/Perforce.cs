﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using CommandLine;
using K9.Utils;

namespace K9.Setup.Verbs
{
    [Verb("Perforce")]
    public class Perforce : DefaultOptions, IVerb
    {
        [Option('c', "client", Required = true, HelpText = "The client identifier for this workspace.")]
        public string Client { get; set; }

        [Option('p', "port", Required = false, HelpText = "The server host:port.",
            Default = Services.Perforce.Config.DefaultPort)]
        public string Port { get; set; }

        public bool CanExecute()
        {
            if (string.IsNullOrEmpty(Username)) return false;

            return true;
        }

        public bool Execute()
        {
            var workspaceRoot = Core.WorkspaceRoot;
            Log.WriteLine("Working in " + workspaceRoot, Program.Instance.DefaultLogCategory);

            var p4configPath = Path.Combine(workspaceRoot, Services.Perforce.Config.FileName);

            // Create P4 Config
            if (!File.Exists(p4configPath))
            {
                Log.WriteLine("Unable to find p4config.txt! (" + p4configPath + ").",
                    Program.Instance.DefaultLogCategory);
                var fileContents = new StringBuilder();

                fileContents.Append("P4USER=");
                fileContents.AppendLine(Username);
                fileContents.Append("P4PASSWD=");
                fileContents.AppendLine(string.IsNullOrEmpty(Password) ? string.Empty : Password);
                fileContents.Append("P4PORT=");
                fileContents.AppendLine(Port);
                fileContents.Append("P4CLIENT=");
                fileContents.AppendLine(Client);
                File.WriteAllText(p4configPath, fileContents.ToString());

                Log.WriteLine("Please configure the file appropriately.", Program.Instance.DefaultLogCategory);
            }

            // Perforce Client Settings
            var outputLines = new List<string>();

            Log.WriteLine("SET P4CONFIG=" + Services.Perforce.Config.FileName, Program.Instance.DefaultLogCategory);
            var p4ConfigCode = ProcessUtil.ExecuteProcess("p4.exe", workspaceRoot,
                "set P4CONFIG=" + Services.Perforce.Config.FileName, null,
                out outputLines);
            foreach (var Line in outputLines) Log.WriteLine(Line, "P4");
            var envConfigCode = ProcessUtil.ExecuteProcess("setx", workspaceRoot,
                "P4CONFIG \"" + Services.Perforce.Config.FileName + "\"", null, out outputLines);
            foreach (var Line in outputLines) Log.WriteLine(Line, "SETX");

            Log.WriteLine("SET P4IGNORE=" + Services.Perforce.Config.P4Ignore, Program.Instance.DefaultLogCategory);
            var p4IgnoreCode = ProcessUtil.ExecuteProcess("p4.exe", workspaceRoot,
                "set P4IGNORE=" + Services.Perforce.Config.P4Ignore, null,
                out outputLines);
            foreach (var Line in outputLines) Log.WriteLine(Line, "P4");
            var envIgnoreCode = ProcessUtil.ExecuteProcess("setx", workspaceRoot,
                "P4IGNORE \"" + Services.Perforce.Config.P4Ignore + "\"", null,
                out outputLines);
            foreach (var Line in outputLines) Log.WriteLine(Line, "SETX");

            Log.WriteLine("SET net.parallel.max=" + Services.Perforce.Config.MaxParallelConnections,
                Program.Instance.DefaultLogCategory);
            var p4MaxConnectionsCode = ProcessUtil.ExecuteProcess("p4.exe", workspaceRoot,
                "set net.parallel.max=" + Services.Perforce.Config.MaxParallelConnections, null,
                out outputLines);

            return p4ConfigCode + p4IgnoreCode + envConfigCode + envIgnoreCode + p4MaxConnectionsCode == 0;
        }
    }
}