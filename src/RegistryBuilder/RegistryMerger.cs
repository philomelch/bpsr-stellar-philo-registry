using System;
using System.Collections.Generic;
using System.Linq;
using RegistryBuilder.Model;

namespace RegistryBuilder;

/// <summary>Validated manifests found for one listed plugin, plus its resolved detail-page fields.</summary>
internal sealed record DiscoveredPlugin(PluginSource Source, IReadOnlyList<ReleaseManifest> Manifests, PluginPresentation? Presentation = null);

/// <summary>Merges freshly discovered releases into the published plugins.json. Pure: no I/O.
/// Rules from manifest-standard.md: history is append-only (a version already published keeps its
/// bytes forever), versions are newest first, and plugin-level fields follow the newest release,
/// except the detail-page fields (media, guide, icon), which follow sources.json.
/// The one deliberate exception: versions withdrawn in sources.json are removed (rollback).</summary>
internal static class RegistryMerger
{
    public static PluginsFile Merge(PluginsFile current, IReadOnlyList<DiscoveredPlugin> discovered)
    {
        var entries = new List<PluginEntry>();
        foreach (var plugin in discovered)
        {
            var existing = current.Plugins.FirstOrDefault(entry => entry.Id == plugin.Source.Id);
            var merged = MergeOne(existing, plugin);
            if (merged is not null) entries.Add(merged);
        }
        return new PluginsFile(entries.OrderBy(entry => entry.Id, StringComparer.Ordinal).ToList());
    }

    private static PluginEntry? MergeOne(PluginEntry? existing, DiscoveredPlugin plugin)
    {
        var source = plugin.Source;
        var versions = (existing?.Versions ?? Array.Empty<PluginVersion>())
            .Where(version => !source.IsWithdrawn(version.Version))
            .ToDictionary(version => version.Version, StringComparer.Ordinal);
        var manifests = plugin.Manifests.Where(manifest => !source.IsWithdrawn(manifest.Release.Version)).ToList();

        foreach (var manifest in manifests)
        {
            var incoming = manifest.Release;
            if (versions.TryGetValue(incoming.Version, out var published))
            {
                if (!string.Equals(published.Sha256, incoming.Sha256, StringComparison.Ordinal))
                    throw new RegistryException(
                        $"{plugin.Source.Id} {incoming.Version} was already published with sha256 {published.Sha256} " +
                        $"but its release now says {incoming.Sha256}. Published versions are immutable; release a new version instead.");
                continue;
            }
            versions.Add(incoming.Version, incoming);
        }

        if (versions.Count == 0) return null;   // listed, but nothing (still) released

        var ordered = versions.Values.OrderByDescending(version => System.Version.Parse(version.Version)).ToList();
        var newest = manifests.OrderByDescending(manifest => System.Version.Parse(manifest.Release.Version)).FirstOrDefault();
        var entry = newest is null
            ? existing! with { Versions = ordered }
            : new PluginEntry(newest.Id, newest.Name, newest.Description, newest.Author, newest.Tags, newest.Homepage,
                Media: null, GuideUrl: null, IconUrl: null, ordered);
        var presentation = plugin.Presentation ?? PluginPresentation.None;
        return entry with { Media = presentation.Media, GuideUrl = presentation.GuideUrl, IconUrl = presentation.IconUrl };
    }
}
