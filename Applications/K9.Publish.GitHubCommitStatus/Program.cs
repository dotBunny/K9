// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using K9.Core;
using Octokit;

namespace K9.Publish.GitHubCommitStatus;


internal static class Program
{
    static void Main()
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
            github.Repository.Status.Create(provider.RepositoryOwner, provider.RepositoryName,
                provider.CommitHash, status);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}


