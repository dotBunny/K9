// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Threading;
using K9.Core;
using K9.Core.Utils;

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

            // Ensure that the data path exists
            FileUtil.EnsureFolderHierarchyExists(provider.Config.DataRoot);

            // Setup exit logic
            Log.WriteLine("Press CTRL+C to Exit");
            Console.CancelKeyPress += delegate (object? _, ConsoleCancelEventArgs e)
            {
                Log.WriteLine("Cancel Requested ...");
                e.Cancel = true;
                s_Alive = false;
            };

            // Loop until otherwise
            while (s_Alive)
            {
                // Check if we have a local workspace checked out

                // Update the local workspace

                // Ensure that the workspace ignore file will ignore the git repository .git folder

                // Check if we have the git repository checked out into the workspace

                // Check for update to the git repository

                // If update, get it, reconcile the p4 workspace

                // Commit to perforce as new CL with message from template PerforceCommitMessageTemplate

                Log.WriteLine("TICK");

                // Sleep till next check
                Thread.Sleep(provider.Config.CheckSleep * 1000);
            }
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}