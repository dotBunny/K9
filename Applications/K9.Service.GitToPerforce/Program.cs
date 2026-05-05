// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Threading;
using K9.Core;
using K9.Core.Utils;
using K9.Services.Git;
using K9.Services.Perforce;

namespace K9.Service.GitToPerforce;

internal static class Program
{
    static bool s_Alive = true;

    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "GIT2P4",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new GitToPerforceProvider());

        try
        {
            GitToPerforceProvider provider = (GitToPerforceProvider)framework.ProgramProvider;
            if (provider.Config == null)
            {
                Log.WriteLine("The CONFIG is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            // Useful for testing
            if (framework.Arguments.HasOverrideArgument("DATA-DIR"))
            {
                provider.Config.DataRoot = framework.Arguments.GetOverrideArgument("DATA-DIR");
            }

            // Ensure that the data path exists
            FileUtil.EnsureFolderHierarchyExists(provider.Config.DataRoot);

            // Setup exit logic
            Log.WriteLine("Press CTRL+C to Exit");
            Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs e)
            {
                Log.WriteLine("Cancel Requested ...");
                e.Cancel = true;
                s_Alive = false;
            };

            // Establish some of our settings
            string workspaceFolder = Path.Combine(provider.Config.DataRoot, "workspace");
            FileUtil.EnsureFolderHierarchyExists(workspaceFolder);

            string repoFolder = Path.Combine(workspaceFolder, provider.Config.GitRepositoryRelativeRoot);
            string gitFolder = Path.Combine(repoFolder, ".git");
            PerforceProvider perforceProvider = new(provider.Config.PerforceUsername,
                provider.Config.PerforceClientName, provider.Config.PerforcePort);

            // START: PREAMBLE

            // Before we get into our monitored loop going to check a bunch of things
            if (!CheckPerforceConnection(perforceProvider, provider.Config))
            {
                Log.WriteLine("Issue connecting with Perforce; stopping preamble.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            // Check that the client is created
            perforceProvider.ClientExists(provider.Config.PerforceClientName, out bool hasClient);
            if (!hasClient)
            {
                Spec clientSpec = new();

                clientSpec.SetField("Client", provider.Config.PerforceClientName);
                if(!string.IsNullOrEmpty(provider.Config.PerforceUsername!))
                {
                    clientSpec.SetField("Owner", provider.Config.PerforceUsername);
                }
                clientSpec.SetField("Host", Environment.MachineName);
                clientSpec.SetField("Description", "K9.Service.GitToPerforce");
                clientSpec.SetField("Root", workspaceFolder);
                clientSpec.SetField("Stream", provider.Config.PerforceWorkspaceStreamName);
                clientSpec.SetField("Options", "clobber rmdir");


                Log.WriteLine("Creating client " + provider.Config.PerforceClientName, ILogOutput.LogType.Info);
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
                Log.WriteLine($"Perforce client({provider.Config.PerforceClientName}) exists.",
                    ILogOutput.LogType.Info);
            }

            // Check if we have the git repository checked out into the workspace BEFORE we pull anything
            if (!Directory.Exists(gitFolder))
            {
                FileUtil.EnsureFolderHierarchyExists(repoFolder);
                GitProvider.CheckoutRepo(provider.Config.GitRepositoryUrl, repoFolder, provider.Config.GitBranch);
            }

            // We need to get our last sync of the workspace
            perforceProvider.Sync(workspaceFolder + @"/...#head");

            // Bring our repo back to a prestine state, because we are going to do a reconcile
            GitProvider.Cleanup(repoFolder);

            Log.WriteLine($"Set perforce ignore file {provider.Config.PerforceIgnoreFile}.", ILogOutput.LogType.Info);
            perforceProvider.SimpleCommand("set P4IGNORE=" + provider.Config.PerforceIgnoreFile);

            bool shouldReconcile = true;

            // END: PREAMBLE

            // Monitor Logic
            while (s_Alive)
            {
                // Check for Git update
                string localCommitHash = GitProvider.GetLocalCommit(repoFolder);
                string? remoteCommitHash = GitProvider.GetRemoteCommit(repoFolder, provider.Config.GitBranch);

                // Check that we have a valid remote hash
                if (string.IsNullOrEmpty(remoteCommitHash) || remoteCommitHash.Length < 39)
                {
                    Log.WriteLine($"Weird RemoteCommitHash {remoteCommitHash}; waiting for next cycle ...", ILogOutput.LogType.Error);

                    // Sleep till next check
                    Thread.Sleep(provider.Config.CheckSleep * 1000);
                    continue;
                }

                // Check if there is an update needed because the hashes are different
                if (localCommitHash != remoteCommitHash)
                {
                    Log.WriteLine(
                        $"Depot needs updating as the local {localCommitHash} differs from {remoteCommitHash}.",
                        ILogOutput.LogType.Info);

                    GitProvider.UpdateRepo(repoFolder, provider.Config.GitBranch, null);
                    GitProvider.Cleanup(repoFolder); // ensure we clean up things
                    shouldReconcile = true;
                }
                else
                {
                    Log.WriteLine(
                        $"Workspace repository ({localCommitHash}) matches remote repository ({remoteCommitHash}) commit.",
                        ILogOutput.LogType.Info);
                }

                if(shouldReconcile)
                {
                    // Ensure that we are logged in and able to do things
                    if (!CheckPerforceConnection(perforceProvider, provider.Config))
                    {
                        Log.WriteLine("Issue with Perforce connection, stopping service.", ILogOutput.LogType.Error);
                        s_Alive = false;
                        break;
                    }

                    // Reconcile to changelist
                    string commitMessage = provider.Config.PerforceCommitMessageTemplate
                        .Replace("$GitPath", provider.Config.GitRepositoryRelativeRoot)
                        .Replace("$GitHash", remoteCommitHash);

                    int changelist = perforceProvider.Reconcile(repoFolder + "/...", commitMessage);

                    if (changelist != -1)
                    {
                        if (!perforceProvider.SimpleCommand("submit -c " + changelist))
                        {
                            Log.WriteLine("We failed to commit changelist: " + changelist, ILogOutput.LogType.Error);
                        }
                    }
                    else
                    {
                        Log.WriteLine("We did not generate a proper changelist despite wanting too?", ILogOutput.LogType.Warning);
                    }

                    shouldReconcile = false;
                }

                // Sleep till next check
                Thread.Sleep(provider.Config.CheckSleep * 1000);
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }

    static bool CheckPerforceConnection(PerforceProvider perforceProvider, GitToPerforceConfig config)
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
                Log.WriteLine("Attempting to login to Perforce has failed: " + perforceMessage, ILogOutput.LogType.Error);
                return false;
            case PerforceProvider.LoginResult.MissingPassword:
                Log.WriteLine("Failed to login to Perforce as the password was missing: " + perforceMessage, ILogOutput.LogType.Error);
                return false;
            case PerforceProvider.LoginResult.IncorrectPassword:
                Log.WriteLine("Failed to login to Perforce as the password was incorrect: " + perforceMessage, ILogOutput.LogType.Error);
                return false;
            default:
                Log.WriteLine("Unknown issue when attempting to update the Perforce connection status.", ILogOutput.LogType.Error);
                return false;
        }
    }
}