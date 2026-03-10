// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using K9.Core;
using K9.Core.Utils;
using K9.Services.Perforce.Records;

namespace K9.Services.Perforce;

public class PerforceProvider
{
    static PerforceConfig? s_CurrentConfig;

    public const string LogCategory = "PERFORCE";

    public static string GetExecutablePath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "p4.exe" : "p4";
    }

    public delegate bool HandleOutputDelegate(OutputLine line);

    delegate bool HandleRecordDelegate(Dictionary<string, string> tags);

    public enum LoginResult
    {
        Failed,
        MissingPassword,
        IncorrectPassword,
        Succeeded
    }

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
    public readonly string ServerAndPort;
    public readonly string UserName;

    public PerforceProvider()
    {
        string? root = WorkspaceUtil.GetWorkspaceRoot();
        if (root != null)
        {
            s_CurrentConfig = new PerforceConfig(Path.Combine(root, SettingsProvider.PerforceConfigFileName));
        }

        if (s_CurrentConfig != null && s_CurrentConfig.IsValid())
        {
            ServerAndPort = s_CurrentConfig.Port;
            UserName = s_CurrentConfig.Username;
            ClientName = s_CurrentConfig.Client;
            return;
        }

        Log.WriteLine("No workspace perforce config present.", ILogOutput.LogType.Error, LogCategory);
        ServerAndPort = string.Empty;
        UserName = string.Empty;
        ClientName = string.Empty;
    }

    public PerforceProvider(PerforceConfig config)
    {
        ServerAndPort = config.Port;
        UserName = config.Username;
        ClientName = config.Client;
    }

    public PerforceProvider(string username, string clientName, string serverAndPort)
    {
        ServerAndPort = serverAndPort;
        UserName = username;
        ClientName = clientName;
    }

    public PerforceProvider OpenClient(string clientName)
    {
        return new PerforceProvider(UserName, clientName, ServerAndPort);
    }

    public bool GetLoggedInState(out bool isLoggedIn)
    {
        List<OutputLine> lines = [];
        bool result = RunCommand("login -s", null, line =>
        {
            lines.Add(line);
            return true;
        }, CommandOptions.None);

        foreach (OutputLine line in lines)
        {
            Log.WriteLine(line.Text, LogCategory);
        }

        if (result)
        {
            isLoggedIn = true;
            return true;
        }

        if (lines[0].Channel == OutputLine.OutputChannel.Error &&
            (lines[0].Text.Contains("P4PASSWD") || lines[0].Text.Contains("has expired")))
        {
            isLoggedIn = false;
            return true;
        }

        isLoggedIn = false;
        return false;
    }

    public LoginResult Login(string? password, out string? errorMessage)
    {
        List<OutputLine> lines = new();
        bool result = RunCommand("login", password, line =>
        {
            lines.Add(line);
            return true;
        }, CommandOptions.IgnoreEnterPassword);
        if (result)
        {
            errorMessage = null;
            return LoginResult.Succeeded;
        }

        errorMessage = string.Join("\n",
            lines.Where(x => x.Channel != OutputLine.OutputChannel.Unknown).Select(x => x.Text));

        if (string.IsNullOrEmpty(password))
        {
            if (lines.Any(x => x.Channel == OutputLine.OutputChannel.Error && x.Text.Contains("EOF")))
            {
                return LoginResult.MissingPassword;
            }
        }
        else
        {
            if (lines.Any(x => x.Channel == OutputLine.OutputChannel.Error && x.Text.Contains("Authentication failed")))
            {
                return LoginResult.IncorrectPassword;
            }
        }

        return LoginResult.Failed;
    }

    public void Logout()
    {
        RunCommand("logout", CommandOptions.None);
    }

    public bool Info(out InfoRecord? info)
    {
        if (!RunCommand("info -s", out List<Dictionary<string, string>>? tagRecords, CommandOptions.NoClient) ||
            tagRecords == null || tagRecords.Count != 1)
        {
            info = null;
            return false;
        }


        info = new InfoRecord(tagRecords[0]);
        return true;
    }

    public bool GetSetting(string name, out string? value)
    {
        if (!RunCommand($"set {name}", out List<string>? lines, CommandOptions.NoChannels) ||
            lines is not { Count: 1 } ||
            lines[0].Length <= name.Length ||
            !lines[0].StartsWith(name, StringComparison.InvariantCultureIgnoreCase) || lines[0][name.Length] != '=')
        {
            value = null;
            return false;
        }

        value = lines[0][(name.Length + 1)..];

        int endIndex = value.IndexOf(" (", StringComparison.Ordinal);
        if (endIndex != -1)
        {
            value = value[..endIndex].Trim();
        }

        return true;
    }

    public bool FindClients(out List<ClientRecord>? clients)
    {
        return RunCommand("clients", out clients, CommandOptions.NoClient);
    }

    public bool FindClients(string forUsername, out List<ClientRecord>? clients)
    {
        return RunCommand($"clients -u \"{forUsername}\"", out clients, CommandOptions.NoClient);
    }

    public bool FindClients(string forUsername, out List<string>? clientNames)
    {
        if (!RunCommand($"clients -u \"{forUsername}\"", out List<string>? lines, CommandOptions.None))
        {
            clientNames = null;
            return false;
        }

        if (lines is { Count: > 0 })
        {
            clientNames = new List<string>();
            foreach (string line in lines)
            {
                string[] tokens = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 5 || tokens[0] != "Client" || tokens[3] != "root")
                {
                    Log.WriteLine($"Couldn't parse client from line '{line}'", LogCategory);
                }
                else
                {
                    clientNames.Add(tokens[1]);
                }
            }

            return true;
        }

        clientNames = null;
        return false;
    }

    public bool CreateClient(Spec client, out string errorMessage)
    {
        List<string> lines = [];
        bool result = RunCommand("client -i", client.ToString(), line =>
        {
            lines.Add(line.Text);
            return line.Channel == OutputLine.OutputChannel.Info;
        }, CommandOptions.None);
        errorMessage = string.Join("\n", lines);

        return result;
    }

    public bool ClientExists(string name, out bool exists)
    {
        if (!RunCommand($"clients -e \"{name}\"", out List<string>? lines, CommandOptions.None))
        {
            exists = false;
            return false;
        }

        if (lines != null)
        {
            exists = lines.Count > 0;
            return true;
        }

        exists = false;
        return false;
    }

    public bool TryGetClientSpec(string clientName, out Spec? spec)
    {
        if (RunCommand($"client -o {clientName}", out List<string>? lines, CommandOptions.None) && lines != null &&
            Spec.TryParse(lines, out spec))
        {
            return true;
        }

        spec = null;
        return false;

    }

    public bool TryGetStreamSpec(string streamName, out Spec? spec)
    {
        if (RunCommand($"stream -o {streamName}", out List<string>? lines, CommandOptions.None) && lines != null &&
            Spec.TryParse(lines, out spec))
        {
            return true;
        }

        spec = null;
        return false;

    }

    public bool FindFiles(string filter, out List<FileRecord>? fileRecords)
    {
        bool result = RunCommand($"fstat \"{filter}\"", out fileRecords, CommandOptions.None);
        if (result && fileRecords != null)
        {
            fileRecords.RemoveAll(x => x.Action != null && x.Action.Contains("delete"));
        }

        return result;
    }

    public bool Print(string depotPath, out List<string>? lines)
    {
        string tempFileName = Path.GetTempFileName();
        try
        {
            if (!PrintToFile(depotPath, tempFileName))
            {
                lines = null;
                return false;
            }

            try
            {
                lines = new List<string>(File.ReadAllLines(tempFileName));
                return true;
            }
            catch
            {
                lines = null;
                return false;
            }
        }
        finally
        {
            try
            {
                File.SetAttributes(tempFileName, FileAttributes.Normal);
                File.Delete(tempFileName);
            }
            catch
            {
                // ignored
            }
        }
    }

    public bool PrintToFile(string depotPath, string outputFileName)
    {
        return RunCommand($"print -q -o \"{outputFileName}\" \"{depotPath}\"",
            CommandOptions.None);
    }

    public bool FileExists(string filter, out bool exists)
    {
        if (RunCommand($"fstat \"{filter}\"", out List<FileRecord>? fileRecords,
                CommandOptions.IgnoreNoSuchFilesError | CommandOptions.IgnoreFilesNotInClientViewError |
                CommandOptions.IgnoreProtectedNamespaceError) && fileRecords != null)
        {
            exists = fileRecords.Exists(x => x.Action == null || !x.Action.Contains("delete"));
            return true;
        }

        exists = false;
        return false;
    }

    public bool FindChanges(string filter, int maxResults, out List<ChangeSummary>? changes)
    {
        return FindChanges([filter], maxResults, out changes);
    }

    public bool FindChanges(IEnumerable<string> filters, int maxResults, out List<ChangeSummary>? changes)
    {
        string arguments = "changes -s submitted -t -L";
        if (maxResults > 0)
        {
            arguments += $" -m {maxResults}";
        }

        arguments = filters.Aggregate(arguments, (current, filter) => current + $" \"{filter}\"");

        if (!RunCommand(arguments, out List<string>? lines, CommandOptions.None) || lines == null)
        {
            changes = null;
            return false;
        }


        changes = new List<ChangeSummary>();
        for (int index = 0; index < lines.Count; index++)
        {
            ChangeSummary? change = TryParseChangeSummary(lines[index]);
            if (change == null)
            {
                Log.WriteLine($"Couldn't parse description from '{lines[index]}'", LogCategory);
            }
            else
            {
                StringBuilder description = new();
                for (; index + 1 < lines.Count; index++)
                {
                    if (lines[index + 1].Length == 0)
                    {
                        description.AppendLine();
                    }
                    else if (lines[index + 1].StartsWith("\t"))
                    {
                        description.AppendLine(lines[index + 1][1..]);
                    }
                    else
                    {
                        break;
                    }
                }

                change.Description = description.ToString().Trim();

                changes.Add(change);
            }
        }

        changes = changes.GroupBy(x => x.Number).Select(x => x.First()).OrderByDescending(x => x.Number).ToList();

        if (maxResults >= 0 && maxResults < changes.Count)
        {
            changes.RemoveRange(maxResults, changes.Count - maxResults);
        }

        return true;
    }

    private static ChangeSummary? TryParseChangeSummary(string line)
    {
        string[] tokens = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (tokens is not ["Change", _, "on", _, _, "by", _])
        {
            return null;
        }

        ChangeSummary change = new();
        if (!int.TryParse(tokens[1], out change.Number) ||
            !DateTime.TryParse(tokens[3] + " " + tokens[4], out change.Date))
        {
            return null;
        }

        int userClientIndex = tokens[6].IndexOf('@');
        if (userClientIndex == -1)
        {
            return null;
        }

        change.User = tokens[6][..userClientIndex];
        change.Client = tokens[6][(userClientIndex + 1)..];
        return change;

    }

    public bool FindFileChanges(string filePath, int maxResults, out List<FileChangeSummary>? changes)
    {
        // ReSharper disable once StringLiteralTypo
        string arguments = "filelog -L -t";
        if (maxResults > 0)
        {
            arguments += $" -m {maxResults}";
        }

        arguments += $" \"{filePath}\"";

        if (!RunCommand(arguments, OutputLine.OutputChannel.TaggedInfo, out List<string>? lines, CommandOptions.None))
        {
            changes = null;
            return false;
        }

        if (lines != null)
        {
            changes = new List<FileChangeSummary>();
            for (int index = 0; index < lines.Count; index++)
            {
                if (!TryParseFileChangeSummary(lines, ref index, out FileChangeSummary? change))
                {
                    Log.WriteLine($"Couldn't parse description from '{lines[index]}'",
                        LogCategory);
                }
                else if (change != null)
                {
                    changes.Add(change);
                }
            }

            return true;
        }

        changes = null;
        return false;
    }

    private static bool TryParseFileChangeSummary(List<string> lines, ref int lineIndex, out FileChangeSummary? change)
    {
        string[] tokens = lines[lineIndex].Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 10 || !tokens[0].StartsWith("#") || tokens[1] != "change" || tokens[4] != "on" ||
            tokens[7] != "by")
        {
            change = null;
            return false;
        }

        FileChangeSummary fileChange = new();
        if (!int.TryParse(tokens[0][1..], out fileChange.Revision) ||
            !int.TryParse(tokens[2], out fileChange.ChangeNumber) ||
            !DateTime.TryParse(tokens[5] + " " + tokens[6], out fileChange.Date))
        {
            change = null;
            return false;
        }

        int userClientIndex = tokens[8].IndexOf('@');
        if (userClientIndex == -1)
        {
            change = null;
            return false;
        }

        fileChange.Action = tokens[3];
        fileChange.Type = tokens[9].Trim('(', ')');
        fileChange.User = tokens[8][..userClientIndex];
        fileChange.Client = tokens[8][(userClientIndex + 1)..];

        StringBuilder description = new();
        for (; lineIndex + 1 < lines.Count; lineIndex++)
        {
            if (lines[lineIndex + 1].Length == 0)
            {
                description.AppendLine();
            }
            else if (lines[lineIndex + 1].StartsWith("\t"))
            {
                description.AppendLine(lines[lineIndex + 1][1..]);
            }
            else
            {
                break;
            }
        }

        fileChange.Description = description.ToString().Trim();

        change = fileChange;
        return true;
    }

    public bool ConvertToClientPath(string fileName, out string? clientFileName)
    {
        if (Where(fileName, out WhereRecord? whereRecord) && whereRecord != null)
        {
            clientFileName = whereRecord.ClientPath;
            return true;
        }

        clientFileName = null;
        return false;
    }

    public bool ConvertToDepotPath(string fileName, out string? depotFileName)
    {
        if (Where(fileName, out WhereRecord? whereRecord) && whereRecord != null)
        {
            depotFileName = whereRecord.DepotPath;
            return true;
        }

        depotFileName = null;
        return false;
    }

    public bool ConvertToLocalPath(string fileName, out string? localFileName)
    {
        if (Where(fileName, out WhereRecord? whereRecord) && whereRecord != null)
        {
            if (whereRecord.LocalPath == null)
            {
                localFileName = null;
                return false;
            }
            localFileName = new FileInfo(whereRecord.LocalPath).GetPathWithCorrectCase();
            return true;
        }

        localFileName = null;
        return false;
    }

    public bool FindStreams(out List<StreamRecord>? outStreams)
    {
        List<Dictionary<string, string>> records = new();
        if (RunCommandWithBinaryOutput("streams", records, CommandOptions.None))
        {
            outStreams = records.Select(x => new StreamRecord(x)).ToList();
            return true;
        }

        outStreams = null;
        return false;
    }

    public bool FindStreams(string filter, out List<string>? foundStreamNames)
    {
        if (!RunCommand($"streams -F \"Stream={filter}\"", out List<string>? lines,
                CommandOptions.None) || lines == null)
        {
            foundStreamNames = null;
            return false;
        }

        List<string> streamNames = [];
        foreach (string line in lines)
        {
            string[] tokens = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2 || tokens[0] != "Stream" || !tokens[1].StartsWith("//"))
            {
                Log.WriteLine($"Unexpected output from stream query: {line}", LogCategory);
                foundStreamNames = null;
                return false;
            }

            streamNames.Add(tokens[1]);
        }

        foundStreamNames = streamNames;

        return true;
    }

    public bool HasOpenFiles()
    {
        bool result = RunCommand("opened -m 1", out List<FileRecord>? records, CommandOptions.None);
        return result && records is { Count: > 0 };
    }

    public bool SwitchStream(string newStream)
    {
        return RunCommand($"client -f -s -S \"{newStream}\" \"{ClientName}\"",
            CommandOptions.None);
    }

    public bool Describe(int changeNumber, out DescribeRecord? record)
    {
        string commandLine = $"describe -s {changeNumber}";

        List<Dictionary<string, string>> records = new();
        if (!RunCommandWithBinaryOutput(commandLine, records, CommandOptions.None))
        {
            record = null;
            return false;
        }

        if (records.Count != 1)
        {
            Log.WriteLine($"Expected 1 record from p4 {commandLine}, got {records.Count}",
                LogCategory);
            record = null;
            return false;
        }

        if (!records[0].TryGetValue("code", out string code) || code != "stat")
        {
            Log.WriteLine(
                $"Unexpected response from p4 {commandLine}: {string.Join(", ",
                    records[0].Select(x => $"( \"{x.Key}\", \"{x.Value}\" )"))}", LogCategory);
            record = null;
            return false;
        }

        record = new DescribeRecord(records[0]);
        return true;
    }

    public bool Where(string filter, out WhereRecord? whereRecord)
    {
        if (!RunCommand($"where \"{filter}\"", out List<FileRecord>? fileRecords, CommandOptions.None))
        {
            whereRecord = null;
            return false;
        }

        if (fileRecords != null)
        {
            fileRecords.RemoveAll(x => x.Unmap);

            if (fileRecords.Count == 0)
            {
                Log.WriteLine($"'{filter}' is not mapped to workspace.", LogCategory);
                whereRecord = null;
                return false;
            }

            if (fileRecords.Count > 1)
            {
                Log.WriteLine(
                    string.Format("File is mapped to {0} locations: {1}", fileRecords.Count,
                        string.Join(", ", fileRecords.Select(x => x.Path))), LogCategory);
                whereRecord = null;
                return false;
            }

            whereRecord = new WhereRecord
            {
                LocalPath = fileRecords[0].Path,
                DepotPath = fileRecords[0].DepotPath,
                ClientPath = fileRecords[0].ClientPath
            };
            return true;
        }

        whereRecord = null;
        return false;
    }

    public bool Have(string filter, out List<FileRecord>? fileRecords)
    {
        return RunCommand($"have \"{filter}\"", out fileRecords,
            CommandOptions.IgnoreFilesNotOnClientError);
    }

    public bool Stat(string filter, out List<FileRecord>? fileRecords)
    {
        return RunCommand($"fstat \"{filter}\"", out fileRecords,
            CommandOptions.IgnoreFilesNotOnClientError | CommandOptions.IgnoreNoSuchFilesError |
            CommandOptions.IgnoreProtectedNamespaceError);
    }

    public bool Stat(string options, List<string> files, out List<FileRecord>? fileRecords)
    {
        StringBuilder arguments = new("fstat");
        if (!string.IsNullOrEmpty(options))
        {
            arguments.AppendFormat(" {0}", options);
        }

        foreach (string file in files)
        {
            arguments.AppendFormat(" \"{0}\"", file);
        }

        return RunCommand(arguments.ToString(), out fileRecords,
            CommandOptions.IgnoreFilesNotOnClientError | CommandOptions.IgnoreNoSuchFilesError |
            CommandOptions.IgnoreProtectedNamespaceError);
    }

    public bool Sync(string filter)
    {
        return RunCommand("sync " + filter, CommandOptions.IgnoreFilesUpToDateError);
    }

    public bool Sync(List<string> fileRevisions, Action<FileRecord> syncOutput, List<string> tamperedFiles,
        bool force, SyncOptions options)
    {
        // Write all the files we want to sync to a temp file
        string tempFileName = Path.GetTempFileName();
        try
        {
            // Write out the temp file
            File.WriteAllLines(tempFileName, fileRevisions);

            // Create a filter to strip all the sync records
            TagRecordParser parser = new(x => syncOutput(new FileRecord(x)));
            StringBuilder commandLine = new();
            commandLine.AppendFormat("-x \"{0}\" -z tag", tempFileName);
            if (options.NumberOfRetries > 0)
            {
                commandLine.AppendFormat(" -r {0}", options.NumberOfRetries);
            }

            if (options.TcpBufferSize > 0)
            {
                // ReSharper disable once StringLiteralTypo
                commandLine.AppendFormat(" -v net.tcpsize={0}", options.TcpBufferSize);
            }

            commandLine.Append(" sync");
            if (force)
            {
                commandLine.AppendFormat(" -f");
            }

            if (options.NumberOfThreads > 1)
            {
                commandLine.AppendFormat(" --parallel=threads={0}", options.NumberOfThreads);
            }

            return RunCommand(commandLine.ToString(), null,
                line => FilterSyncOutput(line, parser, tamperedFiles),
                CommandOptions.NoFailOnErrors | CommandOptions.IgnoreFilesUpToDateError |
                CommandOptions.IgnoreExitCode);
        }
        finally
        {
            // Remove the temp file
            try
            {
                File.Delete(tempFileName);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static bool FilterSyncOutput(OutputLine line, TagRecordParser parser, List<string> tamperedFiles)
    {
        if (line.Channel == OutputLine.OutputChannel.TaggedInfo)
        {
            parser.OutputLine(line.Text);
            return true;
        }

        Log.WriteLine(line.Text, LogCategory);

        const string k_Prefix = "Can't clobber writable file ";
        if (line.Channel != OutputLine.OutputChannel.Error || !line.Text.StartsWith(k_Prefix))
        {
            return line.Channel != OutputLine.OutputChannel.Error;
        }

        tamperedFiles.Add(line.Text[k_Prefix.Length..].Trim());
        return true;

    }

    public void ParseTamperedFile(string line, List<string> tamperedFiles)
    {
        const string k_Prefix = "Can't clobber writable file ";
        if (line.StartsWith(k_Prefix))
        {
            tamperedFiles.Add(line[k_Prefix.Length..].Trim());
        }
    }

    public bool SyncPreview(string filter, int changeNumber, bool onlyFilesInThisChange,
        out List<FileRecord>? fileRecords)
    {
        return RunCommand(
            $"sync -n {filter}@{(onlyFilesInThisChange ? "=" : "")}{changeNumber}",
            out fileRecords,
            CommandOptions.IgnoreFilesUpToDateError | CommandOptions.IgnoreNoSuchFilesError |
            CommandOptions.IgnoreFilesNotInClientViewError);
    }

    public bool ForceSync(string filter)
    {
        return RunCommand($"sync -f \"{filter}\"", CommandOptions.IgnoreFilesUpToDateError);
    }

    public bool ForceSync(string filter, int changeNumber)
    {
        return RunCommand($"sync -f \"{filter}\"@{changeNumber}",
            CommandOptions.IgnoreFilesUpToDateError);
    }

    public bool GetOpenFiles(string filter, out List<FileRecord>? fileRecords)
    {
        return RunCommand($"opened \"{filter}\"", out fileRecords, CommandOptions.None);
    }

    public bool GetUnresolvedFiles(string filter, out List<FileRecord>? fileRecords)
    {
        return RunCommand($"fstat -Ru \"{filter}\"", out fileRecords,
            CommandOptions.IgnoreNoSuchFilesError | CommandOptions.IgnoreFilesNotOpenedOnThisClientError);
    }

    public bool AutoResolveFile(string file)
    {
        return RunCommand($"resolve -am {file}", CommandOptions.None);
    }

    public bool GetActiveStream(out string? streamName)
    {
        if (TryGetClientSpec(ClientName, out Spec? clientSpec) && clientSpec != null)
        {
            streamName = clientSpec.GetField("Stream");
            return streamName != null;
        }

        streamName = null;
        return false;
    }

    private bool RunCommand(string commandLine, out List<FileRecord>? fileRecords, CommandOptions options)
    {
        if (!RunCommand(commandLine, out List<Dictionary<string, string>>? tagRecords, options) || tagRecords == null)
        {
            fileRecords = null;
            return false;
        }

        fileRecords = tagRecords.Select(x => new FileRecord(x)).ToList();
        return true;
    }

    private bool RunCommand(string commandLine, out List<ClientRecord>? clientRecords, CommandOptions options)
    {
        if (!RunCommand(commandLine, out List<Dictionary<string, string>>? tagRecords, options) || tagRecords == null)
        {
            clientRecords = null;
            return false;
        }

        clientRecords = tagRecords.Select(x => new ClientRecord(x)).ToList();
        return true;
    }

    private bool RunCommand(string commandLine, out List<Dictionary<string, string>>? tagRecords,
        CommandOptions options)
    {
        // ReSharper disable once StringLiteralTypo
        if (!RunCommand("-ztag " + commandLine, OutputLine.OutputChannel.TaggedInfo, out List<string>? lines, options))
        {
            tagRecords = null;
            return false;
        }

        if (lines != null)
        {
            List<Dictionary<string, string>> localOutput = new();
            using (TagRecordParser parser = new(record => localOutput.Add(record)))
            {
                foreach (string line in lines)
                {
                    parser.OutputLine(line);
                }
            }

            tagRecords = localOutput;
            return true;
        }

        tagRecords = null;
        return false;
    }

    private bool RunCommand(string commandLine, CommandOptions options)
    {
        bool result = RunCommand(commandLine, out List<string>? lines, options);
        if (lines == null)
        {
            return result;
        }

        foreach (string line in lines)
        {
            Log.WriteLine(line, LogCategory);
        }

        return result;
    }

    private bool RunCommand(string commandLine, out List<string>? lines, CommandOptions options)
    {
        return RunCommand(commandLine, OutputLine.OutputChannel.Info, out lines, options);
    }

    private bool RunCommand(string commandLine, OutputLine.OutputChannel channel, out List<string>? lines,
        CommandOptions options)
    {
        string fullCommandLine = GetFullCommandLine(commandLine, options);
        if (ProcessUtil.Execute(GetExecutablePath(), null, fullCommandLine, null, out List<string> rawOutputLines) !=
            0 &&
            !options.HasFlag(CommandOptions.IgnoreExitCode))
        {
            lines = null;
            foreach (string rawOutputLine in rawOutputLines)
            {
                Log.WriteLine(rawOutputLine, LogCategory);
            }

            return false;
        }

        bool result = true;
        if (options.HasFlag(CommandOptions.NoChannels))
        {
            lines = rawOutputLines;
        }
        else
        {
            List<string> localLines = new();
            foreach (string rawOutputLine in rawOutputLines)
            {
                result &= PerforceUtil.ParseCommandOutput(rawOutputLine,
                    line => FilterOutput(line, channel, localLines), options);
            }

            lines = localLines;
        }

        return result;
    }

    private bool FilterOutput(OutputLine line, OutputLine.OutputChannel filterChannel, List<string> filterLines)
    {
        if (line.Channel == filterChannel)
        {
            filterLines.Add(line.Text);
            return true;
        }

        Log.WriteLine(line.Text, LogCategory);
        return line.Channel != OutputLine.OutputChannel.Error;
    }

    private bool RunCommand(string commandLine, string? input, HandleOutputDelegate handleOutput,
        CommandOptions options)
    {
        string fullCommandLine = GetFullCommandLine(commandLine, options);

        bool result = true;
        int commandStatus = ProcessUtil.Execute(GetExecutablePath(), null, fullCommandLine, input,
            (_, line) =>
            {
                // ReSharper disable once AccessToModifiedClosure - DANGER, but lives in the method scope
                result &= PerforceUtil.ParseCommandOutput(line, handleOutput, options);
            });

        if (commandStatus != 0 && !options.HasFlag(CommandOptions.IgnoreExitCode))
        {
            result = false;
        }

        return result;
    }

    private string GetFullCommandLine(string commandLine, CommandOptions options)
    {
        StringBuilder fullCommandLine = new();
        fullCommandLine.AppendFormat("-p{0} ", ServerAndPort);

        if (!string.IsNullOrEmpty(UserName))
        {
            fullCommandLine.AppendFormat("-u{0} ", UserName);
        }

        if (!options.HasFlag(CommandOptions.NoClient))
        {
            fullCommandLine.AppendFormat("-c{0} ", ClientName);
        }

        if (!options.HasFlag(CommandOptions.NoChannels))
        {
            fullCommandLine.Append("-s ");
        }

        // ReSharper disable once StringLiteralTypo
        fullCommandLine.AppendFormat("-zprog=K9 -zversion={0} ",
            Assembly.GetExecutingAssembly().GetName().Version);
        fullCommandLine.Append(commandLine);

        return fullCommandLine.ToString();
    }

    /// <summary>
    ///     Execute a Perforce command and parse the output as marshaled Python objects.
    ///     This is more robustly defined than the text-based tagged output
    ///     format because it avoids ambiguity when returned fields can have newlines.
    /// </summary>
    private bool RunCommandWithBinaryOutput(string commandLine, List<Dictionary<string, string>> records,
        CommandOptions options)
    {
        return RunCommandWithBinaryOutput(commandLine, record =>
        {
            records.Add(record);
            return true;
        }, options);
    }

    /// <summary>
    ///     Execute a Perforce command and parse the output as marshaled Python objects.
    ///     This is more robustly defined than the text-based tagged output
    ///     format because it avoids ambiguity when returned fields can have newlines.
    /// </summary>
    private bool RunCommandWithBinaryOutput(string commandLine, HandleRecordDelegate handleOutput,
        CommandOptions options)
    {
        // Execute Perforce, consuming the binary output into a memory stream
        MemoryStream memoryStream = new();
        using (Process process = new())
        {
            process.AddDefaultEnvironmentVariables();
            process.StartInfo.FileName = GetExecutablePath();
            process.StartInfo.Arguments =
                GetFullCommandLine("-G " + commandLine, options | CommandOptions.NoChannels);

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            process.StandardOutput.BaseStream.CopyTo(memoryStream);
            process.WaitForExit();
        }

        // Move back to the start of the memory stream
        memoryStream.Position = 0;

        // Parse the records
        using BinaryReader reader = new(memoryStream, Encoding.UTF8);
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            // Check that a dictionary follows
            byte temp = reader.ReadByte();
            if (temp != '{')
            {
                Log.WriteLine("Unexpected data while parsing marshaled output - expected '{'", LogCategory);
                return false;
            }

            // Read all the fields in the record
            Dictionary<string, string> record = new();
            for (;;)
            {
                // Read the next field type. Perforce only outputs string records. A '0' character indicates the end of the dictionary.
                byte keyFieldType = reader.ReadByte();
                if (keyFieldType == '0')
                {
                    break;
                }

                if (keyFieldType != 's')
                {
                    Log.WriteLine(
                        $"Unexpected key field type while parsing marshaled output ({(int)keyFieldType}) - expected 's'", LogCategory);
                    return false;
                }

                // Read the key
                int keyLength = reader.ReadInt32();
                string key = Encoding.UTF8.GetString(reader.ReadBytes(keyLength));

                // Read the value type.
                byte valueFieldType = reader.ReadByte();
                if (valueFieldType == 'i')
                {
                    // An integer
                    string value = reader.ReadInt32().ToString();
                    record.Add(key, value);
                }
                else if (valueFieldType == 's')
                {
                    // A string
                    int valueLength = reader.ReadInt32();
                    string value = Encoding.UTF8.GetString(reader.ReadBytes(valueLength));
                    record.Add(key, value);
                }
                else
                {
                    Log.WriteLine(
                        $"Unexpected value field type while parsing marshalled output ({(int)valueFieldType}) - expected 's'", LogCategory);
                    return false;
                }
            }

            if (!handleOutput(record))
            {
                return false;
            }
        }

        return true;
    }
}