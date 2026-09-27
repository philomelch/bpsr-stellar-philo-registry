using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegistryBuilder.Model;

/// <summary>plugins.json, the file the Stellar launcher reads (StellarResonance/docs/manifest-standard.md § 3).</summary>
internal sealed record PluginsFile(IReadOnlyList<PluginEntry> Plugins)
{
    public static PluginsFile Empty { get; } = new(new List<PluginEntry>());
}

/// <summary>One plugin with its full version history, newest first. Old versions are never dropped
/// (players must be able to roll back). The detail-page fields (media, guide, icon) come from
/// sources.json, not from releases, and are left out of the file when absent.</summary>
internal sealed record PluginEntry(
    string Id,
    string Name,
    string Description,
    string Author,
    IReadOnlyList<string>? Tags,
    string? Homepage,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<PluginMedia>? Media,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? GuideUrl,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? IconUrl,
    IReadOnlyList<PluginVersion> Versions);

/// <summary>One detail-page gallery entry, with an absolute http(s) URL.</summary>
internal sealed record PluginMedia(
    string Type,
    string Url,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Caption = null);

/// <summary>A plugin's resolved detail-page fields, ready for plugins.json.</summary>
internal sealed record PluginPresentation(IReadOnlyList<PluginMedia>? Media, string? GuideUrl, string? IconUrl)
{
    public static PluginPresentation None { get; } = new(null, null, null);
}

/// <summary>One published build. Immutable once published.</summary>
internal sealed record PluginVersion(
    string Version,
    string Date,
    string Dll,
    string DllUrl,
    string Sha256,
    string MinModSystemVersion,
    string? MaxModSystemVersion,
    string SourceRepository,
    string SourceCommit,
    string SourceTag,
    Changelog Changelog);

internal sealed record Changelog(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Changed,
    IReadOnlyList<string> Fixed,
    IReadOnlyList<string> Removed);
