// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using K9.Core;

namespace K9.Publish.CloudflareAssets;

internal static class Program
{
    static async Task Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "CLOUDFLARE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new CloudflareAssetsProvider());

        try
        {
            CloudflareAssetsProvider provider = (CloudflareAssetsProvider)framework.ProgramProvider;

            List<AssetFile> assets = AssetManifest.Build(provider.SourceFolder);
            Log.WriteLine($"{assets.Count} files, {assets.Sum(asset => asset.Size)} bytes, in {provider.SourceFolder}.");

            string? headers = AssetManifest.ReadRootFile(provider.SourceFolder, AssetManifest.HeadersFileName);
            string? redirects = AssetManifest.ReadRootFile(provider.SourceFolder, AssetManifest.RedirectsFileName);
            if (headers != null)
            {
                Log.WriteLine($"{AssetManifest.HeadersFileName} is sent as configuration.");
            }
            if (redirects != null)
            {
                Log.WriteLine($"{AssetManifest.RedirectsFileName} is sent as configuration.");
            }

            string? missingCredentials = provider.GetMissingCredentials();
            if (missingCredentials != null)
            {
                if (!provider.DryRun)
                {
                    throw new InvalidOperationException(missingCredentials);
                }

                Log.WriteLine("DRY-RUN without credentials: hashed only, nothing asked of Cloudflare.", ILogOutput.LogType.Notice);
                return;
            }

            // GetMissingCredentials has proven both.
            using CloudflareClient client = new(provider.AccountId!, provider.ApiToken!);

            UploadSession session = await client.StartUploadSessionAsync(provider.WorkerName, assets);
            int missingCount = session.Buckets.Sum(bucket => bucket.Length);
            Log.WriteLine($"Cloudflare is missing {missingCount} of {assets.Count} files ({session.Buckets.Length} uploads).");

            // Files with the same contents share a hash, and Cloudflare asks for it once.
            Dictionary<string, AssetFile> assetsByHash = assets.GroupBy(asset => asset.Hash).ToDictionary(group => group.Key, group => group.First());

            if (provider.DryRun)
            {
                foreach (string hash in session.Buckets.SelectMany(bucket => bucket))
                {
                    Log.WriteLine($"\t{assetsByHash[hash].ManifestPath}");
                }
                Log.WriteLine("DRY-RUN: nothing uploaded or deployed.", ILogOutput.LogType.Notice);
                return;
            }

            string? completionJwt = session.Buckets.Length == 0 ? session.Jwt : null;
            for (int i = 0; i < session.Buckets.Length; i++)
            {
                List<AssetFile> bucket = session.Buckets[i].Select(hash => assetsByHash[hash]).ToList();
                completionJwt = await client.UploadBucketAsync(session.Jwt, bucket) ?? completionJwt;
                Log.WriteLine($"Uploaded {i + 1}/{session.Buckets.Length}: {bucket.Count} files.");
            }

            if (string.IsNullOrEmpty(completionJwt))
            {
                throw new InvalidOperationException("Every file was uploaded, but Cloudflare returned no completion token.");
            }

            // Two publishes close together can finish out of order; the later one must not put back older files.
            if (long.TryParse(provider.Tag, out long tag))
            {
                string? liveTag = await client.GetLiveTagAsync(provider.WorkerName);
                if (long.TryParse(liveTag, out long live) && live > tag)
                {
                    Log.WriteLine($"{provider.WorkerName} already serves {live}, newer than {tag}; not deploying.", ILogOutput.LogType.Notice);
                    return;
                }
            }

            JsonObject assetConfig = new();
            if (provider.HtmlHandling != null)
            {
                assetConfig["html_handling"] = provider.HtmlHandling;
            }
            if (provider.NotFoundHandling != null)
            {
                assetConfig["not_found_handling"] = provider.NotFoundHandling;
            }
            if (headers != null)
            {
                assetConfig["_headers"] = headers;
            }
            if (redirects != null)
            {
                assetConfig["_redirects"] = redirects;
            }

            Dictionary<string, string> annotations = [];
            if (!string.IsNullOrEmpty(provider.Tag))
            {
                annotations["workers/tag"] = provider.Tag;
            }
            if (!string.IsNullOrEmpty(provider.Message))
            {
                annotations["workers/message"] = provider.Message;
            }

            string? versionId = await client.DeployAsync(provider.WorkerName, completionJwt, assetConfig, annotations, provider.CompatibilityDate);
            Log.WriteLine($"Deployed {provider.WorkerName}, version {versionId ?? "(not reported)"}.", ILogOutput.LogType.Notice);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}
