// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace K9.Publish.CloudflareAssets;

public class AssetFile
{
    /// <summary>
    ///     The path the file is served at, from the root with forward slashes (/assets/css/site.css).
    /// </summary>
    public required string ManifestPath { get; init; }
    public required string FullPath { get; init; }
    public required string Hash { get; init; }
    public required long Size { get; init; }
    public required string ContentType { get; init; }
}

public static class AssetManifest
{
    public const string HeadersFileName = "_headers";
    public const string RedirectsFileName = "_redirects";
    const string k_AssetsIgnoreFileName = ".assetsignore";

    /// <summary>
    ///     Cloudflare's largest asset, 25 MiB.
    /// </summary>
    public const long MaxAssetSize = 25 * 1024 * 1024;

    /// <summary>
    ///     Sent as a part's type when the extension is unknown; Cloudflare then serves the file with no Content-Type.
    /// </summary>
    const string k_NullContentType = "application/null";

    // Read from the root and sent as configuration (as Wrangler does), never served as files.
    static readonly HashSet<string> k_IgnoredAtRoot = new(StringComparer.Ordinal)
    {
        $"/{HeadersFileName}",
        $"/{RedirectsFileName}",
        $"/{k_AssetsIgnoreFileName}"
    };

    static readonly Dictionary<string, string> k_ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "avif", "image/avif" },
        { "css", "text/css; charset=utf-8" },
        { "csv", "text/csv; charset=utf-8" },
        { "gif", "image/gif" },
        { "htm", "text/html; charset=utf-8" },
        { "html", "text/html; charset=utf-8" },
        { "ico", "image/vnd.microsoft.icon" },
        { "jpeg", "image/jpeg" },
        { "jpg", "image/jpeg" },
        { "js", "text/javascript; charset=utf-8" },
        { "json", "application/json" },
        { "map", "application/json" },
        { "md", "text/markdown; charset=utf-8" },
        { "mjs", "text/javascript; charset=utf-8" },
        { "mp3", "audio/mpeg" },
        { "mp4", "video/mp4" },
        { "otf", "font/otf" },
        { "pdf", "application/pdf" },
        { "png", "image/png" },
        { "svg", "image/svg+xml" },
        { "ttf", "font/ttf" },
        { "txt", "text/plain; charset=utf-8" },
        { "wasm", "application/wasm" },
        { "webm", "video/webm" },
        { "webmanifest", "application/manifest+json" },
        { "webp", "image/webp" },
        { "woff", "font/woff" },
        { "woff2", "font/woff2" },
        { "xml", "application/xml" },
        { "zip", "application/zip" }
    };

    /// <summary>
    ///     Walk a folder into the manifest Cloudflare is given, every file hashed.
    /// </summary>
    public static List<AssetFile> Build(string sourceFolder)
    {
        List<AssetFile> assets = [];
        foreach (string fullPath in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
        {
            FileInfo info = new(fullPath);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            string manifestPath = $"/{Path.GetRelativePath(sourceFolder, fullPath).Replace('\\', '/')}";
            if (k_IgnoredAtRoot.Contains(manifestPath))
            {
                continue;
            }

            if (info.Length > MaxAssetSize)
            {
                throw new InvalidOperationException($"{manifestPath} is {info.Length} bytes; Cloudflare takes assets of up to {MaxAssetSize} bytes.");
            }

            string extension = Path.GetExtension(fullPath).TrimStart('.');
            assets.Add(new AssetFile
            {
                ManifestPath = manifestPath,
                FullPath = fullPath,
                Hash = Hash(File.ReadAllBytes(fullPath), extension),
                Size = info.Length,
                ContentType = k_ContentTypes.GetValueOrDefault(extension, k_NullContentType)
            });
        }

        assets.Sort((a, b) => string.CompareOrdinal(a.ManifestPath, b.ManifestPath));
        return assets;
    }

    /// <summary>
    ///     A file's key in Cloudflare's asset store: SHA-256 of its contents as base64 followed by its extension, as hex,
    ///     the first 32 characters. Cloudflare treats it as opaque; this is the form its Direct Upload example uses.
    /// </summary>
    public static string Hash(byte[] contents, string extension)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(Convert.ToBase64String(contents) + extension));
        return Convert.ToHexString(digest).ToLowerInvariant()[..32];
    }

    /// <summary>
    ///     The text of a configuration file at the folder's root (_headers, _redirects), or null when there is none.
    /// </summary>
    public static string? ReadRootFile(string sourceFolder, string fileName)
    {
        string path = Path.Combine(sourceFolder, fileName);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
