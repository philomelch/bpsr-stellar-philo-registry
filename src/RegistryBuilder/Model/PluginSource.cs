using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RegistryBuilder.Model;

/// <summary>One entry of sources.json: which repository is allowed to publish which plugin id.
/// Binding the id to a repo stops one repo from publishing under another plugin's id.
/// <see cref="Withdrawn"/> lists bad releases to pull from plugins.json (the rollback lever).
/// <see cref="Guide"/>, <see cref="Media"/> and <see cref="Icon"/> feed the launcher's detail page;
/// file paths are relative to <c>plugins/&lt;id&gt;/</c> in this repo (see <see cref="PresentationResolver"/>).</summary>
internal sealed record PluginSource(
    string Id,
    string Repository,
    IReadOnlyList<WithdrawnVersion>? Withdrawn = null,
    string? Guide = null,
    IReadOnlyList<MediaSource>? Media = null,
    string? Icon = null)
{
    public bool IsWithdrawn(string version) =>
        Withdrawn?.Any(withdrawn => withdrawn.Version == version) ?? false;

    /// <summary>Whether a release's tag names a withdrawn version. A valid release's tag is always
    /// "v" + its version, so this works without reading (possibly broken) release contents.</summary>
    public bool IsWithdrawnRelease(string tag) => IsWithdrawn(tag.StartsWith('v') ? tag[1..] : tag);
}

/// <summary>A release pulled from the registry. The launcher then treats any installed copy as
/// unknown and requires switching to the newest remaining compatible version: a rollback.</summary>
internal sealed record WithdrawnVersion(string Version, string Reason);

/// <summary>One gallery entry as written in sources.json: exactly one of <see cref="File"/> (a file in
/// this repo) or <see cref="Url"/> (already hosted). <see cref="Type"/> is image, youtube or video.</summary>
internal sealed record MediaSource(string Type, string? File = null, string? Url = null, string? Caption = null);

/// <param name="PublicUrl">Where this repo's files are served, ending in "/", e.g.
/// https://raw.githubusercontent.com/&lt;owner&gt;/&lt;repo&gt;/main/. Required once any plugin lists a
/// guide, media file or icon.</param>
internal sealed record PluginSources(IReadOnlyList<PluginSource> Plugins, string? PublicUrl = null)
{
    public static PluginSources Parse(string json)
    {
        var sources = JsonSerializer.Deserialize<PluginSources>(json, Json.Options)
            ?? throw new RegistryException("sources.json is empty.");

        foreach (var source in sources.Plugins)
            Validate(source);

        var duplicate = sources.Plugins.GroupBy(source => source.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new RegistryException($"sources.json lists id '{duplicate.Key}' more than once.");
        return sources;
    }

    private static void Validate(PluginSource source)
    {
        Formats.Require(Formats.PluginId, source.Id, "sources.json id");
        Formats.Require(Formats.Repository, source.Repository, $"sources.json repository for '{source.Id}'");

        foreach (var withdrawn in source.Withdrawn ?? Array.Empty<WithdrawnVersion>())
        {
            Formats.Require(Formats.Version, withdrawn.Version, $"sources.json withdrawn version for '{source.Id}'");
            if (string.IsNullOrWhiteSpace(withdrawn.Reason))
                throw new RegistryException($"sources.json: withdrawing {source.Id} {withdrawn.Version} needs a reason.");
        }
    }
}
