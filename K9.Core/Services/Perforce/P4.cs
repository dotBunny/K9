﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using K9.Services.Utils;
using K9.Utils;

namespace K9.Services.Perforce
{
    public class P4
    {
        public delegate bool HandleOutputDelegate(OutputLine Line);

        public delegate bool HandleRecordDelegate(Dictionary<string, string> Tags);

        [Flags]
        public enum CommandOptions
        {
            None = 0x0,
            NoClient = 0x1,
            NoFailOnErrors = 0x2,
            NoChannels = 0x4,
            IgnoreFilesUpToDateError = 0x8,
            IgnoreNoSuchFilesError = 0x10,
            IgnoreFilesNotOpenedOnThisClientError = 0x20,
            IgnoreExitCode = 0x40,
            IgnoreFilesNotInClientViewError = 0x80,
            IgnoreEnterPassword = 0x100,
            IgnoreFilesNotOnClientError = 0x200,
            IgnoreProtectedNamespaceError = 0x400
        }

        public readonly string ClientName;
        public readonly string Password;

        public readonly string ServerAndPort;
        public readonly string UserName;

        public P4()
        {
            ServerAndPort = Core.P4Config.Port;
            UserName = Core.P4Config.Username;
            ClientName = Core.P4Config.Client;
            Password = Core.P4Config.Password;
        }

        public P4(Config config)
        {
            ServerAndPort = config.Port;
            UserName = config.Username;
            ClientName = config.Client;
            Password = config.Password;
        }

        public P4(string InUserName, string InClientName, string InServerAndPort, string InPassword)
        {
            ServerAndPort = InServerAndPort;
            UserName = InUserName;
            ClientName = InClientName;
            Password = InPassword;
        }

        public P4 OpenClient(string NewClientName)
        {
            return new P4(UserName, NewClientName, ServerAndPort, Password);
        }

        public bool GetLoggedInState(out bool bIsLoggedIn)
        {
            var Lines = new List<OutputLine>();
            var bResult = RunCommand("login -s", null, Line =>
            {
                Lines.Add(Line);
                return true;
            }, CommandOptions.None);

            foreach (var Line in Lines) Log.WriteLine(Line.Text, "P4");

            if (bResult)
            {
                bIsLoggedIn = true;
                return true;
            }

            if (Lines[0].Channel == OutputChannel.Error &&
                (Lines[0].Text.Contains("P4PASSWD") || Lines[0].Text.Contains("has expired")))
            {
                bIsLoggedIn = false;
                return true;
            }

            bIsLoggedIn = false;
            return false;
        }

        public LoginResult Login(out string ErrorMessage)
        {
            var Lines = new List<OutputLine>();
            var bResult = RunCommand("login", Password, Line =>
            {
                Lines.Add(Line);
                return true;
            }, CommandOptions.IgnoreEnterPassword);
            if (bResult)
            {
                ErrorMessage = null;
                return LoginResult.Succeded;
            }

            ErrorMessage = string.Join("\n", Lines.Where(x => x.Channel != OutputChannel.Unknown).Select(x => x.Text));

            if (string.IsNullOrEmpty(Password))
            {
                if (Lines.Any(x => x.Channel == OutputChannel.Error && x.Text.Contains("EOF")))
                    return LoginResult.MissingPassword;
            }
            else
            {
                if (Lines.Any(x => x.Channel == OutputChannel.Error && x.Text.Contains("Authentication failed")))
                    return LoginResult.IncorrectPassword;
            }

            return LoginResult.Failed;
        }

        public void Logout()
        {
            RunCommand("logout", CommandOptions.None);
        }

        public bool Info(out InfoRecord Info)
        {
            List<Dictionary<string, string>> TagRecords;
            if (!RunCommand("info -s", out TagRecords, CommandOptions.NoClient) || TagRecords.Count != 1)
            {
                Info = null;
                return false;
            }

            Info = new InfoRecord(TagRecords[0]);
            return true;
        }

        public bool GetSetting(string Name, out string Value)
        {
            List<string> Lines;
            if (!RunCommand(string.Format("set {0}", Name), out Lines, CommandOptions.NoChannels) || Lines.Count != 1)
            {
                Value = null;
                return false;
            }

            if (Lines[0].Length <= Name.Length ||
                !Lines[0].StartsWith(Name, StringComparison.InvariantCultureIgnoreCase) || Lines[0][Name.Length] != '=')
            {
                Value = null;
                return false;
            }

            Value = Lines[0].Substring(Name.Length + 1);

            var EndIdx = Value.IndexOf(" (");
            if (EndIdx != -1) Value = Value.Substring(0, EndIdx).Trim();
            return true;
        }

        public bool FindClients(out List<ClientRecord> Clients)
        {
            return RunCommand("clients", out Clients, CommandOptions.NoClient);
        }

        public bool FindClients(string ForUserName, out List<ClientRecord> Clients)
        {
            return RunCommand(string.Format("clients -u \"{0}\"", ForUserName), out Clients, CommandOptions.NoClient);
        }

        public bool FindClients(string ForUserName, out List<string> ClientNames)
        {
            List<string> Lines;
            if (!RunCommand(string.Format("clients -u \"{0}\"", ForUserName), out Lines, CommandOptions.None))
            {
                ClientNames = null;
                return false;
            }

            ClientNames = new List<string>();
            foreach (var Line in Lines)
            {
                var Tokens = Line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (Tokens.Length < 5 || Tokens[0] != "Client" || Tokens[3] != "root")
                    Log.WriteLine(string.Format("Couldn't parse client from line '{0}'", Line), "P4");
                else
                    ClientNames.Add(Tokens[1]);
            }

            return true;
        }

        public bool CreateClient(Spec Client, out string ErrorMessage)
        {
            var Lines = new List<string>();
            var bResult = RunCommand("client -i", Client.ToString(), Line =>
            {
                Lines.Add(Line.Text);
                return Line.Channel == OutputChannel.Info;
            }, CommandOptions.None);
            ErrorMessage = string.Join("\n", Lines);

            return bResult;
        }

        public bool ClientExists(string Name, out bool bExists)
        {
            List<string> Lines;
            if (!RunCommand(string.Format("clients -e \"{0}\"", Name), out Lines, CommandOptions.None))
            {
                bExists = false;
                return false;
            }

            bExists = Lines.Count > 0;
            return true;
        }

        public bool TryGetClientSpec(string ClientName, out Spec Spec)
        {
            List<string> Lines;
            if (!RunCommand(string.Format("client -o {0}", ClientName), out Lines, CommandOptions.None))
            {
                Spec = null;
                return false;
            }

            if (!Spec.TryParse(Lines, out Spec))
            {
                Spec = null;
                return false;
            }

            return true;
        }

        public bool TryGetStreamSpec(string StreamName, out Spec Spec)
        {
            List<string> Lines;
            if (!RunCommand(string.Format("stream -o {0}", StreamName), out Lines, CommandOptions.None))
            {
                Spec = null;
                return false;
            }

            if (!Spec.TryParse(Lines, out Spec))
            {
                Spec = null;
                return false;
            }

            return true;
        }

        public bool FindFiles(string Filter, out List<FileRecord> FileRecords)
        {
            var bResult = RunCommand(string.Format("fstat \"{0}\"", Filter), out FileRecords, CommandOptions.None);
            if (bResult) FileRecords.RemoveAll(x => x.Action != null && x.Action.Contains("delete"));
            return bResult;
        }

        public bool Print(string DepotPath, out List<string> Lines)
        {
            var TempFileName = Path.GetTempFileName();
            try
            {
                if (!PrintToFile(DepotPath, TempFileName))
                {
                    Lines = null;
                    return false;
                }

                try
                {
                    Lines = new List<string>(File.ReadAllLines(TempFileName));
                    return true;
                }
                catch
                {
                    Lines = null;
                    return false;
                }
            }
            finally
            {
                try
                {
                    File.SetAttributes(TempFileName, FileAttributes.Normal);
                    File.Delete(TempFileName);
                }
                catch
                {
                }
            }
        }

        public bool PrintToFile(string DepotPath, string OutputFileName)
        {
            return RunCommand(string.Format("print -q -o \"{0}\" \"{1}\"", OutputFileName, DepotPath),
                CommandOptions.None);
        }

        public bool FileExists(string Filter, out bool bExists)
        {
            List<FileRecord> FileRecords;
            if (RunCommand(string.Format("fstat \"{0}\"", Filter), out FileRecords,
                CommandOptions.IgnoreNoSuchFilesError | CommandOptions.IgnoreFilesNotInClientViewError |
                CommandOptions.IgnoreProtectedNamespaceError))
            {
                bExists = FileRecords.Exists(x => x.Action == null || !x.Action.Contains("delete"));
                return true;
            }

            bExists = false;
            return false;
        }

        public bool FindChanges(string Filter, int MaxResults, out List<ChangeSummary> Changes)
        {
            return FindChanges(new[] {Filter}, MaxResults, out Changes);
        }

        public bool FindChanges(IEnumerable<string> Filters, int MaxResults, out List<ChangeSummary> Changes)
        {
            var Arguments = "changes -s submitted -t -L";
            if (MaxResults > 0) Arguments += string.Format(" -m {0}", MaxResults);
            foreach (var Filter in Filters) Arguments += string.Format(" \"{0}\"", Filter);

            List<string> Lines;
            if (!RunCommand(Arguments, out Lines, CommandOptions.None))
            {
                Changes = null;
                return false;
            }

            Changes = new List<ChangeSummary>();
            for (var Idx = 0; Idx < Lines.Count; Idx++)
            {
                var Change = TryParseChangeSummary(Lines[Idx]);
                if (Change == null)
                {
                    Log.WriteLine(string.Format("Couldn't parse description from '{0}'", Lines[Idx]), "P4");
                }
                else
                {
                    var Description = new StringBuilder();
                    for (; Idx + 1 < Lines.Count; Idx++)
                        if (Lines[Idx + 1].Length == 0)
                            Description.AppendLine();
                        else if (Lines[Idx + 1].StartsWith("\t"))
                            Description.AppendLine(Lines[Idx + 1].Substring(1));
                        else
                            break;
                    Change.Description = Description.ToString().Trim();

                    Changes.Add(Change);
                }
            }

            Changes = Changes.GroupBy(x => x.Number).Select(x => x.First()).OrderByDescending(x => x.Number).ToList();

            if (MaxResults >= 0 && MaxResults < Changes.Count)
                Changes.RemoveRange(MaxResults, Changes.Count - MaxResults);

            return true;
        }

        private ChangeSummary TryParseChangeSummary(string Line)
        {
            var Tokens = Line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (Tokens.Length == 7 && Tokens[0] == "Change" && Tokens[2] == "on" && Tokens[5] == "by")
            {
                var Change = new ChangeSummary();
                if (int.TryParse(Tokens[1], out Change.Number) &&
                    DateTime.TryParse(Tokens[3] + " " + Tokens[4], out Change.Date))
                {
                    var UserClientIdx = Tokens[6].IndexOf('@');
                    if (UserClientIdx != -1)
                    {
                        Change.User = Tokens[6].Substring(0, UserClientIdx);
                        Change.Client = Tokens[6].Substring(UserClientIdx + 1);
                        return Change;
                    }
                }
            }

            return null;
        }

        public bool FindFileChanges(string FilePath, int MaxResults, out List<FileChangeSummary> Changes)
        {
            var Arguments = "filelog -L -t";
            if (MaxResults > 0) Arguments += string.Format(" -m {0}", MaxResults);
            Arguments += string.Format(" \"{0}\"", FilePath);

            List<string> Lines;
            if (!RunCommand(Arguments, OutputChannel.TaggedInfo, out Lines, CommandOptions.None))
            {
                Changes = null;
                return false;
            }

            Changes = new List<FileChangeSummary>();
            for (var Idx = 0; Idx < Lines.Count; Idx++)
            {
                FileChangeSummary Change;
                if (!TryParseFileChangeSummary(Lines, ref Idx, out Change))
                    Log.WriteLine(string.Format("Couldn't parse description from '{0}'", Lines[Idx]), "P4");
                else
                    Changes.Add(Change);
            }

            return true;
        }

        private bool TryParseFileChangeSummary(List<string> Lines, ref int LineIdx, out FileChangeSummary OutChange)
        {
            var Tokens = Lines[LineIdx].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (Tokens.Length != 10 || !Tokens[0].StartsWith("#") || Tokens[1] != "change" || Tokens[4] != "on" ||
                Tokens[7] != "by")
            {
                OutChange = null;
                return false;
            }

            var Change = new FileChangeSummary();
            if (!int.TryParse(Tokens[0].Substring(1), out Change.Revision) ||
                !int.TryParse(Tokens[2], out Change.ChangeNumber) ||
                !DateTime.TryParse(Tokens[5] + " " + Tokens[6], out Change.Date))
            {
                OutChange = null;
                return false;
            }

            var UserClientIdx = Tokens[8].IndexOf('@');
            if (UserClientIdx == -1)
            {
                OutChange = null;
                return false;
            }

            Change.Action = Tokens[3];
            Change.Type = Tokens[9].Trim('(', ')');
            Change.User = Tokens[8].Substring(0, UserClientIdx);
            Change.Client = Tokens[8].Substring(UserClientIdx + 1);

            var Description = new StringBuilder();
            for (; LineIdx + 1 < Lines.Count; LineIdx++)
                if (Lines[LineIdx + 1].Length == 0)
                    Description.AppendLine();
                else if (Lines[LineIdx + 1].StartsWith("\t"))
                    Description.AppendLine(Lines[LineIdx + 1].Substring(1));
                else
                    break;
            Change.Description = Description.ToString().Trim();

            OutChange = Change;
            return true;
        }

        public bool ConvertToClientPath(string FileName, out string ClientFileName)
        {
            WhereRecord WhereRecord;
            if (Where(FileName, out WhereRecord))
            {
                ClientFileName = WhereRecord.ClientPath;
                return true;
            }

            ClientFileName = null;
            return false;
        }

        public bool ConvertToDepotPath(string FileName, out string DepotFileName)
        {
            WhereRecord WhereRecord;
            if (Where(FileName, out WhereRecord))
            {
                DepotFileName = WhereRecord.DepotPath;
                return true;
            }

            DepotFileName = null;
            return false;
        }

        public bool ConvertToLocalPath(string FileName, out string LocalFileName)
        {
            WhereRecord WhereRecord;
            if (Where(FileName, out WhereRecord))
            {
                LocalFileName = FileUtil.GetPathWithCorrectCase(new FileInfo(WhereRecord.LocalPath));
                return true;
            }

            LocalFileName = null;
            return false;
        }

        public bool FindStreams(out List<StreamRecord> OutStreams)
        {
            var Records = new List<Dictionary<string, string>>();
            if (RunCommandWithBinaryOutput("streams", Records, CommandOptions.None))
            {
                OutStreams = Records.Select(x => new StreamRecord(x)).ToList();
                return true;
            }

            OutStreams = null;
            return false;
        }

        public bool FindStreams(string Filter, out List<string> OutStreamNames)
        {
            List<string> Lines;
            if (!RunCommand(string.Format("streams -F \"Stream={0}\"", Filter), out Lines, CommandOptions.None))
            {
                OutStreamNames = null;
                return false;
            }

            var StreamNames = new List<string>();
            foreach (var Line in Lines)
            {
                var Tokens = Line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (Tokens.Length < 2 || Tokens[0] != "Stream" || !Tokens[1].StartsWith("//"))
                {
                    Log.WriteLine(string.Format("Unexpected output from stream query: {0}", Line), "P4");
                    OutStreamNames = null;
                    return false;
                }

                StreamNames.Add(Tokens[1]);
            }

            OutStreamNames = StreamNames;

            return true;
        }

        public bool HasOpenFiles()
        {
            var Records = new List<FileRecord>();
            var bResult = RunCommand("opened -m 1", out Records, CommandOptions.None);
            return bResult && Records.Count > 0;
        }

        public bool SwitchStream(string NewStream)
        {
            return RunCommand(string.Format("client -f -s -S \"{0}\" \"{1}\"", NewStream, ClientName),
                CommandOptions.None);
        }

        public bool Describe(int ChangeNumber, out DescribeRecord Record)
        {
            var CommandLine = string.Format("describe -s {0}", ChangeNumber);

            var Records = new List<Dictionary<string, string>>();
            if (!RunCommandWithBinaryOutput(CommandLine, Records, CommandOptions.None))
            {
                Record = null;
                return false;
            }

            if (Records.Count != 1)
            {
                Log.WriteLine(string.Format("Expected 1 record from p4 {0}, got {1}", CommandLine, Records.Count),
                    "P4");
                Record = null;
                return false;
            }

            string Code;
            if (!Records[0].TryGetValue("code", out Code) || Code != "stat")
            {
                Log.WriteLine(
                    string.Format("Unexpected response from p4 {0}: {1}", CommandLine,
                        string.Join(", ",
                            Records[0].Select(x => string.Format("( \"{0}\", \"{1}\" )", x.Key, x.Value)))), "P4");
                Record = null;
                return false;
            }

            Record = new DescribeRecord(Records[0]);
            return true;
        }

        public bool Where(string Filter, out WhereRecord WhereRecord)
        {
            List<FileRecord> FileRecords;
            if (!RunCommand(string.Format("where \"{0}\"", Filter), out FileRecords, CommandOptions.None))
            {
                WhereRecord = null;
                return false;
            }

            FileRecords.RemoveAll(x => x.Unmap);

            if (FileRecords.Count == 0)
            {
                Log.WriteLine(string.Format("'{0}' is not mapped to workspace.", Filter), "P4");
                WhereRecord = null;
                return false;
            }

            if (FileRecords.Count > 1)
            {
                Log.WriteLine(
                    string.Format("File is mapped to {0} locations: {1}", FileRecords.Count,
                        string.Join(", ", FileRecords.Select(x => x.Path))), "P4");
                WhereRecord = null;
                return false;
            }

            WhereRecord = new WhereRecord();
            WhereRecord.LocalPath = FileRecords[0].Path;
            WhereRecord.DepotPath = FileRecords[0].DepotPath;
            WhereRecord.ClientPath = FileRecords[0].ClientPath;
            return true;
        }

        public bool Have(string Filter, out List<FileRecord> FileRecords)
        {
            return RunCommand(string.Format("have \"{0}\"", Filter), out FileRecords,
                CommandOptions.IgnoreFilesNotOnClientError);
        }

        public bool Stat(string Filter, out List<FileRecord> FileRecords)
        {
            return RunCommand(string.Format("fstat \"{0}\"", Filter), out FileRecords,
                CommandOptions.IgnoreFilesNotOnClientError | CommandOptions.IgnoreNoSuchFilesError |
                CommandOptions.IgnoreProtectedNamespaceError);
        }

        public bool Stat(string Options, List<string> Files, out List<FileRecord> FileRecords)
        {
            var Arguments = new StringBuilder("fstat");
            if (!string.IsNullOrEmpty(Options)) Arguments.AppendFormat(" {0}", Options);
            foreach (var File in Files) Arguments.AppendFormat(" \"{0}\"", File);
            return RunCommand(Arguments.ToString(), out FileRecords,
                CommandOptions.IgnoreFilesNotOnClientError | CommandOptions.IgnoreNoSuchFilesError |
                CommandOptions.IgnoreProtectedNamespaceError);
        }

        public bool Sync(string Filter)
        {
            return RunCommand("sync " + Filter, CommandOptions.IgnoreFilesUpToDateError);
        }

        public bool Sync(List<string> FileRevisions, Action<FileRecord> SyncOutput, List<string> TamperedFiles,
            bool bForce, SyncOptions Options)
        {
            // Write all the files we want to sync to a temp file
            var TempFileName = Path.GetTempFileName();
            try
            {
                // Write out the temp file
                File.WriteAllLines(TempFileName, FileRevisions);

                // Create a filter to strip all the sync records
                bool bResult;
                using (var Parser = new TagRecordParser(x => SyncOutput(new FileRecord(x))))
                {
                    var CommandLine = new StringBuilder();
                    CommandLine.AppendFormat("-x \"{0}\" -z tag", TempFileName);
                    if (Options != null && Options.NumRetries > 0)
                        CommandLine.AppendFormat(" -r {0}", Options.NumRetries);
                    if (Options != null && Options.TcpBufferSize > 0)
                        CommandLine.AppendFormat(" -v net.tcpsize={0}", Options.TcpBufferSize);
                    CommandLine.Append(" sync");
                    if (bForce) CommandLine.AppendFormat(" -f");
                    if (Options != null && Options.NumThreads > 1)
                        CommandLine.AppendFormat(" --parallel=threads={0}", Options.NumThreads);
                    bResult = RunCommand(CommandLine.ToString(), null,
                        Line => FilterSyncOutput(Line, Parser, TamperedFiles),
                        CommandOptions.NoFailOnErrors | CommandOptions.IgnoreFilesUpToDateError |
                        CommandOptions.IgnoreExitCode);
                }

                return bResult;
            }
            finally
            {
                // Remove the temp file
                try
                {
                    File.Delete(TempFileName);
                }
                catch
                {
                }
            }
        }

        private static bool FilterSyncOutput(OutputLine Line, TagRecordParser Parser, List<string> TamperedFiles)
        {
            if (Line.Channel == OutputChannel.TaggedInfo)
            {
                Parser.OutputLine(Line.Text);
                return true;
            }

            Log.WriteLine(Line.Text, "P4");

            const string Prefix = "Can't clobber writable file ";
            if (Line.Channel == OutputChannel.Error && Line.Text.StartsWith(Prefix))
            {
                TamperedFiles.Add(Line.Text.Substring(Prefix.Length).Trim());
                return true;
            }

            return Line.Channel != OutputChannel.Error;
        }

        private void ParseTamperedFile(string Line, List<string> TamperedFiles)
        {
            const string Prefix = "Can't clobber writable file ";
            if (Line.StartsWith(Prefix)) TamperedFiles.Add(Line.Substring(Prefix.Length).Trim());
        }

        public bool SyncPreview(string Filter, int ChangeNumber, bool bOnlyFilesInThisChange,
            out List<FileRecord> FileRecords)
        {
            return RunCommand(
                string.Format("sync -n {0}@{1}{2}", Filter, bOnlyFilesInThisChange ? "=" : "", ChangeNumber),
                out FileRecords,
                CommandOptions.IgnoreFilesUpToDateError | CommandOptions.IgnoreNoSuchFilesError |
                CommandOptions.IgnoreFilesNotInClientViewError);
        }

        public bool ForceSync(string Filter)
        {
            return RunCommand(string.Format("sync -f \"{0}\"", Filter), CommandOptions.IgnoreFilesUpToDateError);
        }

        public bool ForceSync(string Filter, int ChangeNumber)
        {
            return RunCommand(string.Format("sync -f \"{0}\"@{1}", Filter, ChangeNumber),
                CommandOptions.IgnoreFilesUpToDateError);
        }

        public bool GetOpenFiles(string Filter, out List<FileRecord> FileRecords)
        {
            return RunCommand(string.Format("opened \"{0}\"", Filter), out FileRecords, CommandOptions.None);
        }

        public bool GetUnresolvedFiles(string Filter, out List<FileRecord> FileRecords)
        {
            return RunCommand(string.Format("fstat -Ru \"{0}\"", Filter), out FileRecords,
                CommandOptions.IgnoreNoSuchFilesError | CommandOptions.IgnoreFilesNotOpenedOnThisClientError);
        }

        public bool AutoResolveFile(string File)
        {
            return RunCommand(string.Format("resolve -am {0}", File), CommandOptions.None);
        }

        public bool GetActiveStream(out string StreamName)
        {
            Spec ClientSpec;
            if (TryGetClientSpec(ClientName, out ClientSpec))
            {
                StreamName = ClientSpec.GetField("Stream");
                return StreamName != null;
            }

            StreamName = null;
            return false;
        }

        private bool RunCommand(string CommandLine, out List<FileRecord> FileRecords, CommandOptions Options)
        {
            List<Dictionary<string, string>> TagRecords;
            if (!RunCommand(CommandLine, out TagRecords, Options))
            {
                FileRecords = null;
                return false;
            }

            FileRecords = TagRecords.Select(x => new FileRecord(x)).ToList();
            return true;
        }

        private bool RunCommand(string CommandLine, out List<ClientRecord> ClientRecords, CommandOptions Options)
        {
            List<Dictionary<string, string>> TagRecords;
            if (!RunCommand(CommandLine, out TagRecords, Options))
            {
                ClientRecords = null;
                return false;
            }

            ClientRecords = TagRecords.Select(x => new ClientRecord(x)).ToList();
            return true;
        }

        private bool RunCommand(string CommandLine, out List<Dictionary<string, string>> TagRecords,
            CommandOptions Options)
        {
            List<string> Lines;
            if (!RunCommand("-ztag " + CommandLine, OutputChannel.TaggedInfo, out Lines, Options))
            {
                TagRecords = null;
                return false;
            }

            var LocalOutput = new List<Dictionary<string, string>>();
            using (var Parser = new TagRecordParser(Record => LocalOutput.Add(Record)))
            {
                foreach (var Line in Lines) Parser.OutputLine(Line);
            }

            TagRecords = LocalOutput;

            return true;
        }

        private bool RunCommand(string CommandLine, CommandOptions Options)
        {
            List<string> Lines;
            var bResult = RunCommand(CommandLine, out Lines, Options);
            if (Lines != null)
                foreach (var Line in Lines)
                    Log.WriteLine(Line, "P4");
            return bResult;
        }

        private bool RunCommand(string CommandLine, out List<string> Lines, CommandOptions Options)
        {
            return RunCommand(CommandLine, OutputChannel.Info, out Lines, Options);
        }

        private bool RunCommand(string CommandLine, OutputChannel Channel, out List<string> Lines,
            CommandOptions Options)
        {
            var FullCommandLine = GetFullCommandLine(CommandLine, Options);
            Log.WriteLine(string.Format("p4.exe {0}", FullCommandLine), "P4");

            List<string> RawOutputLines;
            if (ProcessUtil.ExecuteProcess("p4.exe", null, FullCommandLine, null, out RawOutputLines) != 0 &&
                !Options.HasFlag(CommandOptions.IgnoreExitCode))
            {
                Lines = null;
                foreach (var RawOutputLine in RawOutputLines) Log.WriteLine(RawOutputLine, "P4");
                return false;
            }

            var bResult = true;
            if (Options.HasFlag(CommandOptions.NoChannels))
            {
                Lines = RawOutputLines;
            }
            else
            {
                var LocalLines = new List<string>();
                foreach (var RawOutputLine in RawOutputLines)
                    bResult &= PerforceUtil.ParseCommandOutput(RawOutputLine,
                        Line => FilterOutput(Line, Channel, LocalLines), Options);
                Lines = LocalLines;
            }

            return bResult;
        }

        private bool FilterOutput(OutputLine Line, OutputChannel FilterChannel, List<string> FilterLines)
        {
            if (Line.Channel == FilterChannel)
            {
                FilterLines.Add(Line.Text);
                return true;
            }

            Log.WriteLine(Line.Text, "P4");
            return Line.Channel != OutputChannel.Error;
        }

        private bool RunCommand(string CommandLine, string Input, HandleOutputDelegate HandleOutput,
            CommandOptions Options)
        {
            var FullCommandLine = GetFullCommandLine(CommandLine, Options);

            var bResult = true;
            Log.WriteLine(string.Format("p4.exe {0}", FullCommandLine), "P4");
            if (ProcessUtil.ExecuteProcess("p4.exe", null, FullCommandLine, Input,
                    Line => { bResult &= PerforceUtil.ParseCommandOutput(Line, HandleOutput, Options); }) != 0 &&
                !Options.HasFlag(CommandOptions.IgnoreExitCode)) bResult = false;
            return bResult;
        }

        private string GetFullCommandLine(string CommandLine, CommandOptions Options)
        {
            var FullCommandLine = new StringBuilder();
            if (ServerAndPort != null) FullCommandLine.AppendFormat("-p{0} ", ServerAndPort);

            if (!string.IsNullOrEmpty(UserName)) FullCommandLine.AppendFormat("-u{0} ", UserName);

            if (!Options.HasFlag(CommandOptions.NoClient) && ClientName != null)
                FullCommandLine.AppendFormat("-c{0} ", ClientName);

            if (!Options.HasFlag(CommandOptions.NoChannels)) FullCommandLine.Append("-s ");

            FullCommandLine.AppendFormat("-zprog=K9 -zversion={0} ",
                Assembly.GetExecutingAssembly().GetName().Version);
            FullCommandLine.Append(CommandLine);

            return FullCommandLine.ToString();
        }

        /// <summary>
        ///     Execute a Perforce command and parse the output as marshalled Python objects. This is more robustly defined than
        ///     the text-based tagged output
        ///     format, because it avoids ambiguity when returned fields can have newlines.
        /// </summary>
        /// <param name="CommandLine">Command line to execute Perforce with</param>
        /// <param name="TaggedOutput">List that receives the output records</param>
        /// <param name="WithClient">Whether to include client information on the command line</param>
        private bool RunCommandWithBinaryOutput(string CommandLine, List<Dictionary<string, string>> Records,
            CommandOptions Options)
        {
            return RunCommandWithBinaryOutput(CommandLine, Record =>
            {
                Records.Add(Record);
                return true;
            }, Options);
        }

        /// <summary>
        ///     Execute a Perforce command and parse the output as marshalled Python objects. This is more robustly defined than
        ///     the text-based tagged output
        ///     format, because it avoids ambiguity when returned fields can have newlines.
        /// </summary>
        /// <param name="CommandLine">Command line to execute Perforce with</param>
        /// <param name="TaggedOutput">List that receives the output records</param>
        /// <param name="WithClient">Whether to include client information on the command line</param>
        private bool RunCommandWithBinaryOutput(string CommandLine, HandleRecordDelegate HandleOutput,
            CommandOptions Options)
        {
            // Execute Perforce, consuming the binary output into a memory stream
            var MemoryStream = new MemoryStream();
            using (var Process = new Process())
            {
                Process.StartInfo.FileName = "p4.exe";
                Process.StartInfo.Arguments =
                    GetFullCommandLine("-G " + CommandLine, Options | CommandOptions.NoChannels);

                Process.StartInfo.RedirectStandardError = true;
                Process.StartInfo.RedirectStandardOutput = true;
                Process.StartInfo.RedirectStandardInput = false;
                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.CreateNoWindow = true;

                Process.Start();

                Process.StandardOutput.BaseStream.CopyTo(MemoryStream);
                Process.WaitForExit();
            }

            // Move back to the start of the memory stream
            MemoryStream.Position = 0;

            // Parse the records
            var Records = new List<Dictionary<string, string>>();
            using (var Reader = new BinaryReader(MemoryStream, Encoding.UTF8))
            {
                while (Reader.BaseStream.Position < Reader.BaseStream.Length)
                {
                    // Check that a dictionary follows
                    var Temp = Reader.ReadByte();
                    if (Temp != '{')
                    {
                        Log.WriteLine("Unexpected data while parsing marshaled output - expected '{'", "P4");
                        return false;
                    }

                    // Read all the fields in the record
                    var Record = new Dictionary<string, string>();
                    for (;;)
                    {
                        // Read the next field type. Perforce only outputs string records. A '0' character indicates the end of the dictionary.
                        var KeyFieldType = Reader.ReadByte();
                        if (KeyFieldType == '0')
                        {
                            break;
                        }

                        if (KeyFieldType != 's')
                        {
                            Log.WriteLine(
                                string.Format(
                                    "Unexpected key field type while parsing marshaled output ({0}) - expected 's'",
                                    (int) KeyFieldType), "P4");
                            return false;
                        }

                        // Read the key
                        var KeyLength = Reader.ReadInt32();
                        var Key = Encoding.UTF8.GetString(Reader.ReadBytes(KeyLength));

                        // Read the value type.
                        var ValueFieldType = Reader.ReadByte();
                        if (ValueFieldType == 'i')
                        {
                            // An integer
                            var Value = Reader.ReadInt32().ToString();
                            Record.Add(Key, Value);
                        }
                        else if (ValueFieldType == 's')
                        {
                            // A string
                            var ValueLength = Reader.ReadInt32();
                            var Value = Encoding.UTF8.GetString(Reader.ReadBytes(ValueLength));
                            Record.Add(Key, Value);
                        }
                        else
                        {
                            Log.WriteLine(
                                string.Format(
                                    "Unexpected value field type while parsing marshalled output ({0}) - expected 's'",
                                    (int) ValueFieldType), "P4");
                            return false;
                        }
                    }

                    if (!HandleOutput(Record)) return false;
                }
            }

            return true;
        }
    }
}