// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;
using Octokit;

namespace K9.Publish.GitHubCommitStatus;

public class GitHubCommitStatusProvider : ProgramProvider
{
    public string? AuthToken { get; set; }
    public string RepositoryOwner { get; set; } = "dotBunny";
    public string RepositoryName { get; set; } = "K9";
    public string? CommitHash { get; set; }

    public CommitState State { get; set; } = CommitState.Success;
    public string? Description { get; set; }
    public string? TargetURL { get; set; }
    public string? Context { get; set; } = "ci/k9";

    public override string GetDescription()
    {
        return "Update a specific GitHub commit status message.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[8];

        lines[0] = new KeyValuePair<string, string>("AUTH-TOKEN", "GitHub Personal Access Token - https://github.com/settings/personal-access-tokens/.");
        lines[1] = new KeyValuePair<string, string>("REPO-OWNER", "The repository's Owner.");
        lines[2] = new KeyValuePair<string, string>("REPO-NAME", "The repository's Name.");
        lines[3] = new KeyValuePair<string, string>("SHA", "The full commit hash (SHA) of the target commit to update status on.");

        lines[4] = new KeyValuePair<string, string>("STATE", "The state of the given description (pending, success, error, failure).");
        lines[5] = new KeyValuePair<string, string>("DESCRIPTION", "The commit status description.");
        lines[6] = new KeyValuePair<string, string>("CONTEXT", "Context information about the commit status.");
        lines[7] = new KeyValuePair<string, string>("URL", "Any URL that the commit status should link to.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("AUTH-TOKEN") || string.IsNullOrEmpty(args.GetOverrideArgument("AUTH-TOKEN")))
        {
            Log.WriteLine("AUTH-TOKEN is required (---AUTH-TOKEN=SomeTokenLookingString");
            return false;
        }

        if (!args.HasOverrideArgument("REPO-OWNER") || string.IsNullOrEmpty(args.GetOverrideArgument("REPO-OWNER")))
        {
            Log.WriteLine("REPO-OWNER is required (---REPO-OWNER=dotBunny");
            return false;
        }

        if (!args.HasOverrideArgument("REPO-NAME")|| string.IsNullOrEmpty(args.GetOverrideArgument("REPO-NAME")))
        {
            Log.WriteLine("REPO-NAME is required (---REPO-NAME=K9");
            return false;
        }

        if (!args.HasOverrideArgument("SHA") || string.IsNullOrEmpty(args.GetOverrideArgument("SHA")))
        {
            Log.WriteLine("SHA is required (---SHA=9e52d9a1800f9fb2d8d7208a75d1f3ba3436a544");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        AuthToken = args.GetOverrideArgument("AUTH-TOKEN");
        RepositoryOwner = args.GetOverrideArgument("REPO-OWNER");
        RepositoryName = args.GetOverrideArgument("REPO-NAME");
        CommitHash = args.GetOverrideArgument("SHA");

        if (args.HasOverrideArgument("STATE"))
        {
            string state = args.GetOverrideArgument("STATE");
            State = state.ToLower() switch
            {
                "pending" => CommitState.Pending,
                "e" or "error" => CommitState.Error,
                "fail" or "failure" => CommitState.Failure,
                _ => CommitState.Success
            };
        }

        if (args.HasOverrideArgument("DESCRIPTION"))
        {
            Description = args.GetOverrideArgument("DESCRIPTION");
        }

        if (args.HasOverrideArgument("CONTEXT"))
        {
            Context = args.GetOverrideArgument("CONTEXT");
        }

        if (args.HasOverrideArgument("URL"))
        {
            TargetURL = args.GetOverrideArgument("URL");
        }

        base.ParseArguments(args);
    }
}