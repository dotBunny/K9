// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Threading.Tasks;
using K9.Core;
using Octokit;

namespace K9.Publish.GitHubCommitStatus;


internal static class Program
{
    static async Task Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "COMMITSTATUS",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new GitHubCommitStatusProvider());

        try
        {
            GitHubCommitStatusProvider provider = (GitHubCommitStatusProvider)framework.ProgramProvider;

            GitHubClient github = new(new ProductHeaderValue("K9"))
            {
                Credentials = new Credentials(provider.AuthToken)
            };



            NewCommitStatus status = new()
            {
                State = provider.State,
                Context = provider.Context,
                TargetUrl = provider.TargetURL,
                Description = provider.Description,
            };

            // Send update
            CommitStatus? commandResult = await github.Repository.Status.Create(provider.RepositoryOwner, provider.RepositoryName,
                provider.CommitHash, status);
            Log.WriteLine($"Commit ({provider.CommitHash}) updated with: {status.State}");
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}


