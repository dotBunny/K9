// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using K9.Core;
using K9.Core.LogOutputs;
using K9.Core.Utils;
using K9.Services.Perforce;

namespace K9.Service.PerforceBackup;

internal static class Program
{
    static bool s_Alive = true;

    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "P4BACKUP",
                LogOutputs = [new ConsoleLogOutput()]
            }, new PerforceBackupProvider());

        try
        {
            PerforceBackupProvider provider = (PerforceBackupProvider)framework.ProgramProvider;
            if (provider.Config == null)
            {
                Log.WriteLine("The CONFIG is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            PerforceBackupConfig config = provider.Config;

            // Useful for testing
            if (framework.Arguments.HasOverrideArgument("DATA-DIR"))
            {
                config.DataRoot = framework.Arguments.GetOverrideArgument("DATA-DIR");
            }

            bool runNow = framework.Arguments.HasBaseArgument("RUN-NOW");
            bool skipUpload = framework.Arguments.HasBaseArgument("NO-UPLOAD");

            // Ensure that the data paths exist
            string workspaceFolder = config.GetWorkspaceFolder();
            FileUtil.EnsureFolderHierarchyExists(config.DataRoot);
            FileUtil.EnsureFolderHierarchyExists(workspaceFolder);
            FileUtil.EnsureFolderHierarchyExists(config.GetStagingFolder());

            if (!string.IsNullOrEmpty(config.LogFolder))
            {
                Log.AddLogOutput(new FileLogOutput(config.LogFolder!, "K9.Service.PerforceBackup"));
            }

            // Setup exit logic
            Log.WriteLine("Press CTRL+C to Exit");
            Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs e)
            {
                Log.WriteLine("Cancel Requested ...");
                e.Cancel = true;
                s_Alive = false;
            };

            // START: PREAMBLE

            // There is no point discovering at 3am that there is nothing on this machine to compress with.
            string? sevenZipPath = CompressionUtil.GetSevenZipPath(config.SevenZipPath);
            if (sevenZipPath == null)
            {
                Log.WriteLine("Unable to find 7-Zip; install it or set SevenZipPath in the configuration.",
                    ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }
            Log.WriteLine($"Using 7-Zip @ {sevenZipPath}.", ILogOutput.LogType.Info);

            PerforceProvider perforceProvider = new(config.PerforceUsername, config.PerforceClientName,
                config.PerforcePort);

            if (!CheckPerforceConnection(perforceProvider, config))
            {
                Log.WriteLine("Issue connecting with Perforce; stopping preamble.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            // Check that the client is created
            perforceProvider.ClientExists(config.PerforceClientName, out bool hasClient);
            if (!hasClient)
            {
                Spec clientSpec = new();

                clientSpec.SetField("Client", config.PerforceClientName);
                if (!string.IsNullOrEmpty(config.PerforceUsername!))
                {
                    clientSpec.SetField("Owner", config.PerforceUsername);
                }
                clientSpec.SetField("Host", Environment.MachineName);
                clientSpec.SetField("Description", "K9.Service.PerforceBackup");
                clientSpec.SetField("Root", workspaceFolder);
                clientSpec.SetField("Stream", config.PerforceWorkspaceStreamName);
                clientSpec.SetField("Options", "clobber rmdir");

                Log.WriteLine("Creating client " + config.PerforceClientName, ILogOutput.LogType.Info);
                if (!perforceProvider.CreateClient(clientSpec, out string errorMessage))
                {
                    Log.WriteLine($"Unable to create client({errorMessage}); stopping preamble.",
                        ILogOutput.LogType.Error);
                    framework.Shutdown();
                    return;
                }
            }
            else
            {
                Log.WriteLine($"Perforce client({config.PerforceClientName}) exists.", ILogOutput.LogType.Info);
            }

            // A share being down right now is not fatal, we check again when the backup actually runs.
            if (!skipUpload && !EnsureNetworkAccess(config))
            {
                Log.WriteLine($"Unable to reach {config.GetNetworkRoot()} during startup, will try again at backup time.",
                    ILogOutput.LogType.Warning);
            }

            PerforceBackupState state = PerforceBackupState.Get(config.GetStateFile());
            if (state.LastSuccessUtc != null)
            {
                Log.WriteLine(
                    $"Last successful backup was {state.LastArchive} @ {state.LastSuccessUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}.",
                    ILogOutput.LogType.Info);
            }
            Log.WriteLine($"Backups are scheduled daily @ {config.BackupTimeOfDay}.", ILogOutput.LogType.Info);

            // END: PREAMBLE

            // Monitor Logic
            while (s_Alive)
            {
                // Ensure that we are logged in and able to do things
                if (!CheckPerforceConnection(perforceProvider, config))
                {
                    Log.WriteLine("Issue with Perforce connection, stopping service.", ILogOutput.LogType.Error);
                    s_Alive = false;
                    break;
                }

                Log.WriteLine($"Syncing workspace @ {workspaceFolder} ...", ILogOutput.LogType.Info);
                if (!perforceProvider.Sync(workspaceFolder + "/...#head"))
                {
                    Log.WriteLine("The workspace sync did not complete cleanly.", ILogOutput.LogType.Warning);
                }

                if (runNow || ShouldBackup(DateTime.Now, config, state))
                {
                    runNow = false;
                    state.LastAttemptUtc = DateTime.UtcNow;

                    if (RunBackup(perforceProvider, config, state, sevenZipPath, skipUpload, out string archiveFileName))
                    {
                        if (skipUpload)
                        {
                            // Enough to stop this session looping, but the next launch must not think
                            // a diagnostic run took care of today.
                            state.LastSuccessUtc = DateTime.UtcNow;
                            Log.WriteLine("NO-UPLOAD was set, the state file was left untouched.",
                                ILogOutput.LogType.Notice);
                        }
                        else
                        {
                            state.LastSuccessUtc = DateTime.UtcNow;
                            state.LastArchive = archiveFileName;
                            state.Write(config.GetStateFile());
                            Log.WriteLine($"Backup {archiveFileName} completed.", ILogOutput.LogType.Notice);
                        }
                    }
                    else
                    {
                        state.Write(config.GetStateFile());
                        Log.WriteLine(
                            config.RetryOnFailure
                                ? "The backup failed, it will be retried on the next cycle."
                                : "The backup failed, it will be attempted again tomorrow.",
                            ILogOutput.LogType.Error);
                    }
                }

                // Sleep till next check
                Sleep(config.SyncSleep);
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }

    static bool ShouldBackup(DateTime now, PerforceBackupConfig config, PerforceBackupState state)
    {
        if (!config.TryGetBackupTimeOfDay(out TimeSpan timeOfDay))
        {
            return false;
        }

        DateTime scheduled = now.Date.Add(timeOfDay);
        if (now < scheduled)
        {
            return false;
        }

        // Today is already taken care of.
        if (state.LastSuccessUtc != null && state.LastSuccessUtc.Value.ToLocalTime() >= scheduled)
        {
            return false;
        }

        // Without retries enabled a failed attempt has to wait for tomorrow's window.
        return config.RetryOnFailure || state.LastAttemptUtc == null ||
               state.LastAttemptUtc.Value.ToLocalTime() < scheduled;
    }

    static bool RunBackup(PerforceProvider perforceProvider, PerforceBackupConfig config, PerforceBackupState state,
        string sevenZipPath, bool skipUpload, out string archiveFileName)
    {
        string stagingFolder = config.GetStagingFolder();
        FileUtil.EnsureFolderHierarchyExists(stagingFolder);

        // A previous run may have already built and verified an archive that simply could not be copied.
        archiveFileName = state.PendingArchive ?? string.Empty;
        string stagingPath = string.IsNullOrEmpty(archiveFileName)
            ? string.Empty
            : Path.Combine(stagingFolder, archiveFileName);

        if (!string.IsNullOrEmpty(stagingPath) && File.Exists(stagingPath))
        {
            Log.WriteLine($"Reusing {archiveFileName}, left in staging by a previous attempt.",
                ILogOutput.LogType.Notice);
        }
        else
        {
            state.PendingArchive = null;

            string sourceFolder = config.GetBackupSourceFolder();
            if (!Directory.Exists(sourceFolder))
            {
                Log.WriteLine($"The backup source folder was not found @ {sourceFolder}.", ILogOutput.LogType.Error);
                return false;
            }

            // Anything still in staging is the wreckage of a previous run and is only costing us disk space.
            ClearStaging(stagingFolder);

            if (!HasEnoughFreeSpace(config, stagingFolder))
            {
                return false;
            }

            archiveFileName = config.GetArchiveFileName(GetSyncedChangelist(perforceProvider, config));
            stagingPath = Path.Combine(stagingFolder, archiveFileName);

            Log.WriteLine($"Creating {archiveFileName} from {sourceFolder} ...", ILogOutput.LogType.Notice);
            Timer compressionTimer = new();
            if (!CompressionUtil.Create7z(sourceFolder, "*", stagingPath, sevenZipPath, config.SevenZipArguments))
            {
                return false;
            }

            if (!File.Exists(stagingPath))
            {
                Log.WriteLine($"7-Zip reported success but no archive was found @ {stagingPath}.",
                    ILogOutput.LogType.Error);
                return false;
            }

            Log.WriteLine(
                $"Created {archiveFileName} ({GetHumanReadableSize(new FileInfo(stagingPath).Length)}) in {compressionTimer.GetElapsedSeconds()} seconds.",
                ILogOutput.LogType.Info);

            if (config.VerifyArchive && !CompressionUtil.Test7z(stagingPath, sevenZipPath))
            {
                Log.WriteLine("The created archive failed verification, discarding it.", ILogOutput.LogType.Error);
                FileUtil.ForceDeleteFile(stagingPath);
                return false;
            }
        }

        long archiveSize = new FileInfo(stagingPath).Length;

        if (skipUpload)
        {
            Log.WriteLine($"NO-UPLOAD was set, the archive has been left @ {stagingPath}.",
                ILogOutput.LogType.Notice);
            return true;
        }

        // Hours may have passed since the preamble, prove the share is there before we lean on it.
        if (!EnsureNetworkAccess(config))
        {
            state.PendingArchive = archiveFileName;
            Log.WriteLine(
                $"Unable to reach {config.GetNetworkRoot()}, keeping {archiveFileName} in staging for the next attempt.",
                ILogOutput.LogType.Error);
            return false;
        }

        string targetFolder = config.GetNetworkTargetFolder();

        // Copy under a partial name so an interrupted transfer can never be mistaken for a good backup.
        string partialPath = Path.Combine(targetFolder, archiveFileName + ".part");
        string finalPath = Path.Combine(targetFolder, archiveFileName);

        Timer transferTimer = new();
        try
        {
            FileUtil.EnsureFolderHierarchyExists(targetFolder);

            File.Copy(stagingPath, partialPath, true);

            long copiedSize = new FileInfo(partialPath).Length;
            if (copiedSize != archiveSize)
            {
                Log.WriteLine($"The copied archive is {copiedSize} bytes, but the source is {archiveSize} bytes.",
                    ILogOutput.LogType.Error);
                FileUtil.ForceDeleteFile(partialPath);
                state.PendingArchive = archiveFileName;
                return false;
            }

            FileUtil.ForceDeleteFile(finalPath);
            File.Move(partialPath, finalPath);
        }
        catch (Exception e)
        {
            Log.WriteLine($"Failed to copy the archive to {targetFolder} ({e.Message}).", ILogOutput.LogType.Error);
            FileUtil.ForceDeleteFile(partialPath);
            state.PendingArchive = archiveFileName;
            return false;
        }

        // The rate comes back empty for a copy that finished inside a millisecond.
        string transferRate = transferTimer.TransferRate(archiveSize);
        Log.WriteLine(
            string.IsNullOrEmpty(transferRate)
                ? $"Copied {archiveFileName} ({GetHumanReadableSize(archiveSize)}) to {targetFolder}."
                : $"Copied {archiveFileName} ({GetHumanReadableSize(archiveSize)}) to {targetFolder} @ {transferRate}.",
            ILogOutput.LogType.Info);

        FileUtil.ForceDeleteFile(stagingPath);
        state.PendingArchive = null;

        Prune(config, targetFolder);

        return true;
    }

    static void Prune(PerforceBackupConfig config, string targetFolder)
    {
        try
        {
            DirectoryInfo directory = new(targetFolder);
            string searchPattern = config.GetArchiveSearchPattern();

            // Sweep up anything a previous failed transfer left behind while we are in here.
            foreach (FileInfo partial in directory.GetFiles(searchPattern + ".part"))
            {
                Log.WriteLine($"Removing orphaned partial transfer {partial.Name}.", ILogOutput.LogType.Warning);
                FileUtil.ForceDeleteFile(partial.FullName);
            }

            if (config.KeepCount <= 0)
            {
                return;
            }

            List<FileInfo> archives = directory.GetFiles(searchPattern)
                .Where(x => x.Extension.Equals(".7z", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.LastWriteTimeUtc)
                .ToList();

            for (int i = config.KeepCount; i < archives.Count; i++)
            {
                Log.WriteLine($"Pruning {archives[i].Name}.", ILogOutput.LogType.Info);
                FileUtil.ForceDeleteFile(archives[i].FullName);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine($"Unable to prune old backups in {targetFolder} ({e.Message}).",
                ILogOutput.LogType.Warning);
        }
    }

    static void ClearStaging(string stagingFolder)
    {
        try
        {
            foreach (string leftover in Directory.GetFiles(stagingFolder, "*.7z"))
            {
                Log.WriteLine($"Removing leftover staged archive {Path.GetFileName(leftover)}.",
                    ILogOutput.LogType.Warning);
                FileUtil.ForceDeleteFile(leftover);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine($"Unable to clear the staging folder {stagingFolder} ({e.Message}).",
                ILogOutput.LogType.Warning);
        }
    }

    static bool HasEnoughFreeSpace(PerforceBackupConfig config, string stagingFolder)
    {
        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(stagingFolder));
            if (string.IsNullOrEmpty(root))
            {
                return true;
            }

            long availableBytes = new DriveInfo(root!).AvailableFreeSpace;
            Log.WriteLine($"{GetHumanReadableSize(availableBytes)} available on {root} for staging.",
                ILogOutput.LogType.Info);

            if (config.MinimumFreeSpaceGigabytes <= 0)
            {
                return true;
            }

            long requiredBytes = config.MinimumFreeSpaceGigabytes * 1024L * 1024L * 1024L;
            if (availableBytes >= requiredBytes)
            {
                return true;
            }

            Log.WriteLine(
                $"Only {GetHumanReadableSize(availableBytes)} available on {root}, {config.MinimumFreeSpaceGigabytes}GB is required.",
                ILogOutput.LogType.Error);
            return false;
        }
        catch (Exception e)
        {
            // Not being able to measure the drive is no reason to skip the backup.
            Log.WriteLine($"Unable to determine the free space for {stagingFolder} ({e.Message}).",
                ILogOutput.LogType.Warning);
            return true;
        }
    }

    static int GetSyncedChangelist(PerforceProvider perforceProvider, PerforceBackupConfig config)
    {
        if (perforceProvider.FindChanges($"{config.GetWorkspaceFolder()}/...#have", 1,
                out List<ChangeSummary>? changes) && changes is { Count: > 0 })
        {
            return changes[0].Number;
        }

        return -1;
    }

    static bool EnsureNetworkAccess(PerforceBackupConfig config)
    {
        string networkRoot = config.GetNetworkRoot();
        if (Directory.Exists(networkRoot))
        {
            return true;
        }

        if (string.IsNullOrEmpty(config.NetworkUsername) || string.IsNullOrEmpty(config.NetworkPassword))
        {
            Log.WriteLine($"{networkRoot} is not reachable and no network credentials were provided.",
                ILogOutput.LogType.Error);
            return false;
        }

        Log.WriteLine($"Mapping {config.NetworkShare} ...", ILogOutput.LogType.Info);
        ProcessLogRedirect logRedirect = new();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Without a drive letter we still authenticate against the share so the UNC path becomes usable.
            string arguments = string.IsNullOrEmpty(config.NetworkMapping)
                ? $"use {config.NetworkShare} /USER:{config.NetworkUsername} {config.NetworkPassword}"
                : $"use {config.NetworkMapping} {config.NetworkShare} /USER:{config.NetworkUsername} {config.NetworkPassword}";

            ProcessUtil.Execute("net", null, arguments, null, logRedirect.GetAction());
        }
        else
        {
            if (string.IsNullOrEmpty(config.NetworkMapping))
            {
                Log.WriteLine("A NetworkMapping mount point is required to mount a share on this platform.",
                    ILogOutput.LogType.Error);
                return false;
            }

            ProcessUtil.Execute("mount", null,
                $"-t cifs -o username={config.NetworkUsername},password={config.NetworkPassword} {config.NetworkShare} {config.NetworkMapping}",
                null, logRedirect.GetAction());
        }

        return Directory.Exists(networkRoot);
    }

    static void Sleep(int seconds)
    {
        // Chunked so a cancel request does not have to wait out the whole interval.
        const int k_ChunkMilliseconds = 500;
        int remainingMilliseconds = seconds * 1000;
        while (s_Alive && remainingMilliseconds > 0)
        {
            int waitMilliseconds = Math.Min(k_ChunkMilliseconds, remainingMilliseconds);
            System.Threading.Thread.Sleep(waitMilliseconds);
            remainingMilliseconds -= waitMilliseconds;
        }
    }

    static string GetHumanReadableSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024d && unitIndex < units.Length - 1)
        {
            size /= 1024d;
            unitIndex++;
        }

        return $"{size.ToString("0.##", CultureInfo.InvariantCulture)}{units[unitIndex]}";
    }

    static bool CheckPerforceConnection(PerforceProvider perforceProvider, PerforceBackupConfig config)
    {
        perforceProvider.GetLoggedInState(out bool isConnected);
        if (isConnected)
        {
            return true;
        }

        switch (perforceProvider.Login(config.PerforcePassword, out string? perforceMessage))
        {
            case PerforceProvider.LoginResult.Succeeded:
                return true;
            case PerforceProvider.LoginResult.Failed:
                Log.WriteLine("Attempting to login to Perforce has failed: " + perforceMessage,
                    ILogOutput.LogType.Error);
                return false;
            case PerforceProvider.LoginResult.MissingPassword:
                Log.WriteLine("Failed to login to Perforce as the password was missing: " + perforceMessage,
                    ILogOutput.LogType.Error);
                return false;
            case PerforceProvider.LoginResult.IncorrectPassword:
                Log.WriteLine("Failed to login to Perforce as the password was incorrect: " + perforceMessage,
                    ILogOutput.LogType.Error);
                return false;
            default:
                Log.WriteLine("Unknown issue when attempting to update the Perforce connection status.",
                    ILogOutput.LogType.Error);
                return false;
        }
    }
}
