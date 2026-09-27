using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RegistryBuilder.GitHub;
using RegistryBuilder.Model;

namespace RegistryBuilder;

/// <summary>One registry update: for every listed plugin, read its releases, validate each
/// manifest, verify the DLL hash of versions not published yet, resolve its detail-page files, and
/// merge into plugins.json. Any problem throws, and the published file stays as it was.</summary>
internal sealed class RegistryUpdater
{
    private const long MaxManifestBytes = 256 * 1024;
    private const long MaxDllBytes = 64 * 1024 * 1024;
    private const string ManifestAsset = "manifest.json";

    private readonly IReleaseFeed _feed;
    private readonly Func<PluginSource, PluginPresentation> _presentationFor;
    private readonly Action<string> _log;

    /// <param name="presentationFor">Resolves a plugin's guide, media and icon (a <see cref="PresentationResolver"/>).</param>
    public RegistryUpdater(IReleaseFeed feed, Func<PluginSource, PluginPresentation> presentationFor, Action<string> log)
    {
        _feed = feed;
        _presentationFor = presentationFor;
        _log = log;
    }

    public async Task<PluginsFile> UpdateAsync(PluginSources sources, PluginsFile current,
        IReadOnlySet<string> officialIds, CancellationToken ct)
    {
        var discovered = new List<DiscoveredPlugin>();
        foreach (var source in sources.Plugins)
        {
            // The launcher lets later registries override earlier ones by id, so reusing an
            // official id would silently replace the official plugin for our users.
            if (officialIds.Contains(source.Id))
                throw new RegistryException($"Plugin id '{source.Id}' is already used by the official registry; pick another id.");

            var published = current.Plugins.FirstOrDefault(entry => entry.Id == source.Id);
            var manifests = await ReadManifestsAsync(source, published, ct).ConfigureAwait(false);
            discovered.Add(new DiscoveredPlugin(source, manifests, _presentationFor(source)));
        }
        return RegistryMerger.Merge(current, discovered);
    }

    private async Task<IReadOnlyList<ReleaseManifest>> ReadManifestsAsync(PluginSource source, PluginEntry? published, CancellationToken ct)
    {
        var manifests = new List<ReleaseManifest>();
        foreach (var release in await _feed.ListReleasesAsync(source.Repository, ct).ConfigureAwait(false))
        {
            // Before reading the manifest: withdrawing a release whose manifest is broken must still
            // unblock the registry.
            if (source.IsWithdrawnRelease(release.Tag))
            {
                _log($"{source.Id} {release.Tag} is withdrawn in sources.json; not published.");
                continue;
            }

            if (!release.Assets.TryGetValue(ManifestAsset, out var manifestUrl))
            {
                _log($"::warning::{source.Repository}@{release.Tag} has no {ManifestAsset}; skipped.");
                continue;
            }

            var bytes = await _feed.DownloadAsync(manifestUrl, MaxManifestBytes, ct).ConfigureAwait(false);
            var manifest = JsonSerializer.Deserialize<ReleaseManifest>(bytes, Json.Options)
                ?? throw new RegistryException($"{source.Repository}@{release.Tag}: empty {ManifestAsset}.");
            ManifestValidator.Validate(manifest, source, release);

            var isNew = published?.Versions.All(version => version.Version != manifest.Release.Version) ?? true;
            if (isNew) await VerifyDllAsync(manifest, release, ct).ConfigureAwait(false);
            manifests.Add(manifest);
        }
        return manifests;
    }

    // Only new versions are downloaded: published ones are already pinned by hash in plugins.json.
    private async Task VerifyDllAsync(ReleaseManifest manifest, PublishedRelease release, CancellationToken ct)
    {
        var dll = await _feed.DownloadAsync(release.Assets[manifest.Release.Dll], MaxDllBytes, ct).ConfigureAwait(false);
        var actual = Convert.ToHexString(SHA256.HashData(dll)).ToLowerInvariant();
        if (!string.Equals(actual, manifest.Release.Sha256, StringComparison.Ordinal))
            throw new RegistryException(
                $"{manifest.Id} {manifest.Release.Version}: DLL sha256 is {actual}, manifest says {manifest.Release.Sha256}.");
        _log($"{manifest.Id} {manifest.Release.Version}: new version verified.");
    }
}
