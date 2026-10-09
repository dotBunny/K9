// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.Publish.CloudflareAssets;

public class CloudflareAssetsProvider : ProgramProvider
{
    const string k_ApiTokenVariable = "CLOUDFLARE_API_TOKEN";
    const string k_AccountIdVariable = "CLOUDFLARE_ACCOUNT_ID";

    static readonly string[] k_HtmlHandlingValues = ["auto-trailing-slash", "force-trailing-slash", "drop-trailing-slash", "none"];
    static readonly string[] k_NotFoundHandlingValues = ["none", "404-page", "single-page-application"];

    public string SourceFolder { get; private set; } = string.Empty;
    public string WorkerName { get; private set; } = string.Empty;
    public string? AccountId { get; private set; }
    public string? ApiToken { get; private set; }
    public string? CredentialsPath { get; private set; }
    public string? Tag { get; private set; }
    public string? Message { get; private set; }
    public string? HtmlHandling { get; private set; }
    public string? NotFoundHandling { get; private set; }
    public string CompatibilityDate { get; private set; } = "2026-10-09";
    public bool DryRun { get; private set; }

    public override string GetDescription()
    {
        return "Publish a folder as a Cloudflare Worker's static assets, uploading only the files Cloudflare does not already have.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[11];

        lines[0] = new KeyValuePair<string, string>("SOURCE", "The folder to publish; its contents become the Worker's assets.");
        lines[1] = new KeyValuePair<string, string>("WORKER", "The name of the Worker to deploy to.");
        lines[2] = new KeyValuePair<string, string>("ACCOUNT-ID", $"The Cloudflare account id. Defaults to the {k_AccountIdVariable} environment variable.");
        lines[3] = new KeyValuePair<string, string>("API-TOKEN", $"A Cloudflare API token with Workers Scripts: Edit. Defaults to the {k_ApiTokenVariable} environment variable.");
        lines[4] = new KeyValuePair<string, string>("CREDENTIALS", "A file containing the Cloudflare API token, allowing for it not to show in logs.");
        lines[5] = new KeyValuePair<string, string>("TAG", "Recorded on the version as workers/tag. When numeric, a live version with a higher tag is never replaced.");
        lines[6] = new KeyValuePair<string, string>("MESSAGE", "Recorded on the version as workers/message.");
        lines[7] = new KeyValuePair<string, string>("HTML-HANDLING", $"The assets' html_handling ({string.Join(", ", k_HtmlHandlingValues)}).");
        lines[8] = new KeyValuePair<string, string>("NOT-FOUND-HANDLING", $"The assets' not_found_handling ({string.Join(", ", k_NotFoundHandlingValues)}).");
        lines[9] = new KeyValuePair<string, string>("COMPATIBILITY-DATE", "The Worker's compatibility date (yyyy-MM-dd).");
        lines[10] = new KeyValuePair<string, string>("DRY-RUN", "Hash the files and, given credentials, report what Cloudflare is missing; upload and deploy nothing.");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (!args.HasOverrideArgument("SOURCE") || string.IsNullOrEmpty(args.GetOverrideArgument("SOURCE")))
        {
            Log.WriteLine("SOURCE is required (---SOURCE=C:\\Site\\_site)");
            return false;
        }

        if (!Directory.Exists(args.GetOverrideArgument("SOURCE")))
        {
            Log.WriteLine($"SOURCE does not exist: {args.GetOverrideArgument("SOURCE")}");
            return false;
        }

        if (!args.HasOverrideArgument("WORKER") || string.IsNullOrEmpty(args.GetOverrideArgument("WORKER")))
        {
            Log.WriteLine("WORKER is required (---WORKER=my-worker)");
            return false;
        }

        // Missing credentials are not checked here: refused arguments exit 0, and a publish whose secret never arrived
        // must fail. Program throws for them instead (GetMissingCredentials).

        if (args.HasOverrideArgument("HTML-HANDLING") && Array.IndexOf(k_HtmlHandlingValues, args.GetOverrideArgument("HTML-HANDLING")) < 0)
        {
            Log.WriteLine($"HTML-HANDLING must be one of: {string.Join(", ", k_HtmlHandlingValues)}");
            return false;
        }

        if (args.HasOverrideArgument("NOT-FOUND-HANDLING") && Array.IndexOf(k_NotFoundHandlingValues, args.GetOverrideArgument("NOT-FOUND-HANDLING")) < 0)
        {
            Log.WriteLine($"NOT-FOUND-HANDLING must be one of: {string.Join(", ", k_NotFoundHandlingValues)}");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        SourceFolder = Path.GetFullPath(args.GetOverrideArgument("SOURCE"));
        WorkerName = args.GetOverrideArgument("WORKER");
        AccountId = ResolveAccountId(args);
        ApiToken = ResolveApiToken(args);
        DryRun = args.HasOverrideArgument("DRY-RUN");

        if (args.HasOverrideArgument("CREDENTIALS"))
        {
            CredentialsPath = args.GetOverrideArgument("CREDENTIALS");
        }

        if (args.HasOverrideArgument("TAG"))
        {
            Tag = args.GetOverrideArgument("TAG");
        }

        if (args.HasOverrideArgument("MESSAGE"))
        {
            Message = args.GetOverrideArgument("MESSAGE");
        }

        if (args.HasOverrideArgument("HTML-HANDLING"))
        {
            HtmlHandling = args.GetOverrideArgument("HTML-HANDLING");
        }

        if (args.HasOverrideArgument("NOT-FOUND-HANDLING"))
        {
            NotFoundHandling = args.GetOverrideArgument("NOT-FOUND-HANDLING");
        }

        if (args.HasOverrideArgument("COMPATIBILITY-DATE"))
        {
            CompatibilityDate = args.GetOverrideArgument("COMPATIBILITY-DATE");
        }

        base.ParseArguments(args);
    }

    /// <summary>
    ///     What stops a real publish, or null when the token and the account are both known.
    /// </summary>
    public string? GetMissingCredentials()
    {
        if (CredentialsPath != null && !File.Exists(CredentialsPath))
        {
            return $"CREDENTIALS does not exist: {CredentialsPath}";
        }

        if (string.IsNullOrEmpty(ApiToken))
        {
            return $"API-TOKEN is required (---API-TOKEN=SomeTokenLookingString), or CREDENTIALS, or the {k_ApiTokenVariable} environment variable";
        }

        if (string.IsNullOrEmpty(AccountId))
        {
            return $"ACCOUNT-ID is required (---ACCOUNT-ID=0123456789abcdef0123456789abcdef), or the {k_AccountIdVariable} environment variable";
        }

        return null;
    }

    static string? ResolveApiToken(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("CREDENTIALS"))
        {
            string path = args.GetOverrideArgument("CREDENTIALS");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }

        if (args.HasOverrideArgument("API-TOKEN"))
        {
            return args.GetOverrideArgument("API-TOKEN");
        }

        return Environment.GetEnvironmentVariable(k_ApiTokenVariable)?.Trim();
    }

    static string? ResolveAccountId(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("ACCOUNT-ID"))
        {
            return args.GetOverrideArgument("ACCOUNT-ID");
        }

        return Environment.GetEnvironmentVariable(k_AccountIdVariable)?.Trim();
    }
}
