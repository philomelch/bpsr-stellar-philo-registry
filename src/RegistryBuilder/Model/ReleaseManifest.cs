using System.Collections.Generic;

namespace RegistryBuilder.Model;

/// <summary>manifest.json (schema 1), attached to each plugin GitHub Release by the plugin repo's
/// release workflow (its tools/ReleaseTool). Mirrors that tool's output exactly.</summary>
internal sealed record ReleaseManifest(
    int Schema,
    string Id,
    string Name,
    string Description,
    string Author,
    IReadOnlyList<string>? Tags,
    string? Homepage,
    PluginVersion Release)
{
    public const int SupportedSchema = 1;
}
