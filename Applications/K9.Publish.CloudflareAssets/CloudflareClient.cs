// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using K9.Core;

namespace K9.Publish.CloudflareAssets;

public class CloudflareApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class UploadSession
{
    /// <summary>
    ///     Authorizes the uploads for an hour; when there is nothing to upload it is already the completion token.
    /// </summary>
    public required string Jwt { get; init; }

    /// <summary>
    ///     The hashes Cloudflare does not have, grouped by Cloudflare into the requests they should be uploaded in.
    /// </summary>
    public required string[][] Buckets { get; init; }
}

/// <summary>
///     The Workers static assets Direct Upload API:
///     https://developers.cloudflare.com/workers/static-assets/direct-upload/
/// </summary>
public sealed class CloudflareClient : IDisposable
{
    const string k_BaseAddress = "https://api.cloudflare.com/client/v4/";
    const int k_MaxAttempts = 5;

    readonly string m_AccountId;
    readonly string m_ApiToken;
    readonly HttpClient m_Http;

    public CloudflareClient(string accountId, string apiToken)
    {
        m_AccountId = accountId;
        m_ApiToken = apiToken;
        m_Http = new HttpClient { BaseAddress = new Uri(k_BaseAddress), Timeout = TimeSpan.FromMinutes(10) };
        m_Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("K9.Publish.CloudflareAssets", "1.0"));
    }

    public void Dispose()
    {
        m_Http.Dispose();
    }

    /// <summary>
    ///     Give Cloudflare the manifest and learn which files it is missing.
    /// </summary>
    public async Task<UploadSession> StartUploadSessionAsync(string workerName, IEnumerable<AssetFile> assets)
    {
        JsonObject manifest = new();
        foreach (AssetFile asset in assets)
        {
            manifest[asset.ManifestPath] = new JsonObject { ["hash"] = asset.Hash, ["size"] = asset.Size };
        }

        string body = new JsonObject { ["manifest"] = manifest }.ToJsonString();
        JsonNode? result = await SendAsync(HttpMethod.Post, $"accounts/{m_AccountId}/workers/scripts/{workerName}/assets-upload-session",
            () => new StringContent(body, Encoding.UTF8, "application/json"), m_ApiToken);

        string? jwt = result?["jwt"]?.GetValue<string>();
        if (string.IsNullOrEmpty(jwt))
        {
            throw new InvalidOperationException("Cloudflare started an upload session without a token.");
        }

        string[][] buckets = result?["buckets"]?.AsArray()
            .Select(bucket => bucket!.AsArray().Select(hash => hash!.GetValue<string>()).ToArray())
            .ToArray() ?? [];

        return new UploadSession { Jwt = jwt, Buckets = buckets };
    }

    /// <summary>
    ///     Upload one bucket of files; returns the completion token once Cloudflare has every file in the manifest.
    /// </summary>
    public async Task<string?> UploadBucketAsync(string uploadJwt, IReadOnlyList<AssetFile> bucket)
    {
        // Read once; the content is rebuilt for each attempt, since a sent request's content cannot be sent again.
        List<(AssetFile Asset, string Base64)> payload = [];
        foreach (AssetFile asset in bucket)
        {
            payload.Add((asset, Convert.ToBase64String(await File.ReadAllBytesAsync(asset.FullPath))));
        }

        JsonNode? result = await SendAsync(HttpMethod.Post, $"accounts/{m_AccountId}/workers/assets/upload?base64=true", () =>
        {
            MultipartFormDataContent form = new();
            foreach ((AssetFile asset, string base64) in payload)
            {
                StringContent part = new(base64);
                part.Headers.ContentType = MediaTypeHeaderValue.Parse(asset.ContentType);
                form.Add(part, asset.Hash, asset.Hash);
            }
            return form;
        }, uploadJwt);

        return result?["jwt"]?.GetValue<string>();
    }

    /// <summary>
    ///     The workers/tag of the version serving most of the Worker's traffic, or null when there is none.
    /// </summary>
    public async Task<string?> GetLiveTagAsync(string workerName)
    {
        JsonNode? deployments;
        try
        {
            deployments = await SendAsync(HttpMethod.Get, $"accounts/{m_AccountId}/workers/scripts/{workerName}/deployments", null, m_ApiToken);
        }
        catch (CloudflareApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        // The latest deployment is first.
        JsonNode? latest = deployments?["deployments"]?.AsArray().FirstOrDefault();
        string? versionId = latest?["versions"]?.AsArray()
            .OrderByDescending(version => version?["percentage"]?.GetValue<double>() ?? 0)
            .Select(version => version?["version_id"]?.GetValue<string>())
            .FirstOrDefault();
        if (string.IsNullOrEmpty(versionId))
        {
            return null;
        }

        JsonNode? version = await SendAsync(HttpMethod.Get, $"accounts/{m_AccountId}/workers/scripts/{workerName}/versions/{versionId}", null, m_ApiToken);
        return version?["annotations"]?["workers/tag"]?.GetValue<string>();
    }

    /// <summary>
    ///     Make a version of the Worker from the uploaded assets, with no script, and deploy it to all of its traffic.
    /// </summary>
    /// <returns>The new version's id.</returns>
    public async Task<string?> DeployAsync(string workerName, string completionJwt, JsonObject assetConfig,
        Dictionary<string, string> annotations, string compatibilityDate)
    {
        JsonObject annotationsNode = new();
        foreach (KeyValuePair<string, string> annotation in annotations)
        {
            annotationsNode[annotation.Key] = annotation.Value;
        }

        // An assets-only Worker has no main_module and no module parts, as Wrangler uploads one.
        string metadata = new JsonObject
        {
            ["assets"] = new JsonObject { ["jwt"] = completionJwt, ["config"] = assetConfig },
            ["annotations"] = annotationsNode,
            ["compatibility_date"] = compatibilityDate
        }.ToJsonString();

        JsonNode? result = await SendAsync(HttpMethod.Put, $"accounts/{m_AccountId}/workers/scripts/{workerName}?excludeScript=true", () =>
        {
            MultipartFormDataContent form = new();
            StringContent part = new(metadata, Encoding.UTF8, "application/json");
            form.Add(part, "metadata");
            return form;
        }, m_ApiToken);

        // Despite its name, this is the version's id.
        return result?["deployment_id"]?.GetValue<string>();
    }

    /// <summary>
    ///     Send a request, retrying what may pass on a second try (a dropped connection, a 429, a 5xx), and return the
    ///     result of Cloudflare's envelope.
    /// </summary>
    async Task<JsonNode?> SendAsync(HttpMethod method, string path, Func<HttpContent>? createContent, string bearer)
    {
        for (int attempt = 1; ; attempt++)
        {
            using HttpRequestMessage request = new(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (createContent != null)
            {
                request.Content = createContent();
            }

            HttpResponseMessage response;
            try
            {
                response = await m_Http.SendAsync(request);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && attempt < k_MaxAttempts)
            {
                TimeSpan delay = Backoff(attempt);
                Log.WriteLine($"{method} {StripQuery(path)} failed ({e.Message}); retrying in {delay.TotalSeconds}s.", ILogOutput.LogType.Warning);
                await Task.Delay(delay);
                continue;
            }

            using (response)
            {
                string text = await response.Content.ReadAsStringAsync();
                bool retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
                if (retryable && attempt < k_MaxAttempts)
                {
                    TimeSpan delay = response.Headers.RetryAfter?.Delta ?? Backoff(attempt);
                    Log.WriteLine($"{method} {StripQuery(path)} answered {(int)response.StatusCode}; retrying in {delay.TotalSeconds}s.", ILogOutput.LogType.Warning);
                    await Task.Delay(delay);
                    continue;
                }

                JsonNode? envelope = null;
                try
                {
                    envelope = JsonNode.Parse(text);
                }
                catch (JsonException)
                {
                    // Not Cloudflare's envelope (a proxy's error page); reported below with the status.
                }

                bool success = envelope?["success"]?.GetValue<bool>() ?? false;
                if (!response.IsSuccessStatusCode || !success)
                {
                    throw new CloudflareApiException(response.StatusCode,
                        $"{method} {StripQuery(path)} answered {(int)response.StatusCode}: {DescribeErrors(envelope, text)}");
                }

                return envelope?["result"];
            }
        }
    }

    static TimeSpan Backoff(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
    }

    static string StripQuery(string path)
    {
        int query = path.IndexOf('?');
        return query < 0 ? path : path[..query];
    }

    static string DescribeErrors(JsonNode? envelope, string text)
    {
        JsonArray? errors = envelope?["errors"] as JsonArray;
        if (errors == null || errors.Count == 0)
        {
            return text.Length > 500 ? text[..500] : text;
        }

        return string.Join("; ", errors.Select(error => $"{error?["code"]} {error?["message"]}"));
    }
}
