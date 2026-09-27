using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryBuilder.GitHub;

/// <summary>Reads public releases through the GitHub REST API. <see cref="_api"/> carries the
/// workflow's token (only for rate limits); asset downloads use a separate client with no token,
/// so the token is never sent to the asset CDN.</summary>
internal sealed class GitHubReleaseFeed : IReleaseFeed
{
    private const int PageSize = 100;

    private readonly HttpClient _api;
    private readonly HttpClient _downloads;

    public GitHubReleaseFeed(HttpClient api, HttpClient downloads)
    {
        _api = api;
        _downloads = downloads;
    }

    public async Task<IReadOnlyList<PublishedRelease>> ListReleasesAsync(string repository, CancellationToken ct)
    {
        var releases = new List<PublishedRelease>();
        for (var page = 1; ; page++)
        {
            var url = new Uri($"https://api.github.com/repos/{repository}/releases?per_page={PageSize}&page={page}");
            var batch = await _api.GetFromJsonAsync<List<GitHubRelease>>(url, ct).ConfigureAwait(false)
                ?? new List<GitHubRelease>();
            releases.AddRange(batch.Where(release => !release.Draft && !release.Prerelease).Select(ToPublished));
            if (batch.Count < PageSize) return releases;
        }
    }

    public async Task<byte[]> DownloadAsync(Uri asset, long maxBytes, CancellationToken ct)
    {
        using var response = await _downloads.GetAsync(asset, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maxBytes)
            throw new RegistryException($"{asset} is larger than the {maxBytes}-byte limit.");

        using var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(chunk, ct).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > maxBytes)
                throw new RegistryException($"{asset} is larger than the {maxBytes}-byte limit.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static PublishedRelease ToPublished(GitHubRelease release) =>
        new(release.TagName, release.Assets.ToDictionary(asset => asset.Name, asset => new Uri(asset.BrowserDownloadUrl), StringComparer.Ordinal));

    // Only the fields we use; the API returns many more, which the default options ignore.
    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset> Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}
