using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryBuilder.GitHub;

/// <summary>A published (non-draft, non-prerelease) release and its assets by file name.</summary>
internal sealed record PublishedRelease(string Tag, IReadOnlyDictionary<string, Uri> Assets);

/// <summary>Port over GitHub Releases, so the update logic is testable without the network.</summary>
internal interface IReleaseFeed
{
    Task<IReadOnlyList<PublishedRelease>> ListReleasesAsync(string repository, CancellationToken ct);

    /// <summary>Downloads an asset, refusing anything larger than <paramref name="maxBytes"/>.</summary>
    Task<byte[]> DownloadAsync(Uri asset, long maxBytes, CancellationToken ct);
}
