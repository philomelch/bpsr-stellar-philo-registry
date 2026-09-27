using System;
using System.IO;
using System.Linq;
using RegistryBuilder.Model;

namespace RegistryBuilder;

/// <summary>Turns a sources.json entry's guide, media and icon into the absolute URLs plugins.json
/// publishes, like the official registry does with its <c>plugins/&lt;id&gt;/</c> folders. Files live in
/// this repo under <c>plugins/&lt;id&gt;/</c> and are served from sources.json's <c>publicUrl</c>. Each
/// must exist, stay inside its plugin's folder, have an expected extension and fit the official size
/// limits (guide ≤ 1 MB, media ≤ 25 MB). Anything wrong throws, like every other check, so a broken
/// link is never published.</summary>
internal sealed class PresentationResolver
{
    private const long MaxGuideBytes = 1024 * 1024;
    private const long MaxMediaBytes = 25 * 1024 * 1024;
    private static readonly string[] GuideExtensions = { ".md" };
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
    private static readonly string[] VideoExtensions = { ".mp4", ".webm" };

    private readonly string _repositoryRoot;
    private readonly Uri? _publicUrl;

    /// <param name="repositoryRoot">The registry checkout (the folder holding sources.json).</param>
    /// <param name="publicUrl">sources.json's <c>publicUrl</c>; only needed once a file is listed.</param>
    public PresentationResolver(string repositoryRoot, string? publicUrl)
    {
        _repositoryRoot = repositoryRoot;
        _publicUrl = publicUrl is null ? null : ParsePublicUrl(publicUrl);
    }

    public PluginPresentation Resolve(PluginSource source)
    {
        var media = source.Media?.Select(entry => ResolveMedia(source.Id, entry)).ToList();
        var guide = source.Guide is null
            ? null
            : FileUrl(new PluginFile(source.Id, source.Guide, $"{source.Id} guide"), GuideExtensions, MaxGuideBytes);
        var icon = source.Icon is null ? null : ResolveIcon(source.Id, source.Icon);
        return new PluginPresentation(media, guide, icon);
    }

    private PluginMedia ResolveMedia(string id, MediaSource entry)
    {
        var what = $"{id} media '{entry.File ?? entry.Url}'";
        if ((entry.File is null) == (entry.Url is null))
            throw new RegistryException($"{what}: give exactly one of file or url.");

        var file = entry.File is null ? null : new PluginFile(id, entry.File, what);
        var url = (entry.Type, file) switch
        {
            ("image", { } image) => FileUrl(image, ImageExtensions, MaxMediaBytes),
            ("video", { } video) => FileUrl(video, VideoExtensions, MaxMediaBytes),
            ("youtube", { }) => throw new RegistryException($"{what}: a youtube entry needs a url, not a file."),
            ("image" or "video" or "youtube", null) => RequireHttps(entry.Url!, what),
            _ => throw new RegistryException($"{what}: type must be image, youtube or video."),
        };
        return new PluginMedia(entry.Type, url, entry.Caption);
    }

    // Like the official registry, the icon is either a file in the plugin's folder or a hosted URL.
    private string ResolveIcon(string id, string icon) =>
        icon.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? RequireHttps(icon, $"{id} icon")
            : FileUrl(new PluginFile(id, icon, $"{id} icon"), ImageExtensions, MaxMediaBytes);

    private string FileUrl(PluginFile file, string[] extensions, long maxBytes)
    {
        Formats.Require(Formats.RelativeFile, file.Path, $"{file.What} path");
        if (!extensions.Contains(Path.GetExtension(file.Path).ToLowerInvariant()))
            throw new RegistryException($"{file.What}: '{file.Path}' must be one of {string.Join(", ", extensions)}.");

        var relative = $"plugins/{file.PluginId}/{file.Path}";
        var info = new FileInfo(Path.Combine(_repositoryRoot, relative));
        if (!info.Exists)
            throw new RegistryException($"{file.What}: {relative} does not exist.");
        if (info.Length > maxBytes)
            throw new RegistryException($"{file.What}: {relative} is {info.Length} bytes; the limit is {maxBytes}.");
        if (_publicUrl is null)
            throw new RegistryException($"sources.json needs a publicUrl to publish {relative}.");
        return new Uri(_publicUrl, relative).AbsoluteUri;
    }

    private static string RequireHttps(string url, string what)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new RegistryException($"{what}: '{url}' must be an absolute https URL.");
        return url;
    }

    private static Uri ParsePublicUrl(string publicUrl)
    {
        RequireHttps(publicUrl, "sources.json publicUrl");
        if (!publicUrl.EndsWith('/'))
            throw new RegistryException($"sources.json publicUrl '{publicUrl}' must end with '/'.");
        return new Uri(publicUrl);
    }

    /// <summary>A path from sources.json, relative to <c>plugins/&lt;PluginId&gt;/</c>; <see cref="What"/>
    /// names it in error messages.</summary>
    private sealed record PluginFile(string PluginId, string Path, string What);
}
