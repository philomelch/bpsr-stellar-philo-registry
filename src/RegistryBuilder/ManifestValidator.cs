using System;
using RegistryBuilder.GitHub;
using RegistryBuilder.Model;

namespace RegistryBuilder;

/// <summary>Checks that a release's manifest.json is well-formed and consistent with where it was
/// found: the id is the one sources.json binds to this repo, every URL and provenance field
/// points back at this exact repo and tag, and the DLL it names is an asset of this release.</summary>
internal static class ManifestValidator
{
    public static void Validate(ReleaseManifest manifest, PluginSource source, PublishedRelease release)
    {
        var where = $"{source.Repository}@{release.Tag}";
        if (manifest.Schema != ReleaseManifest.SupportedSchema)
            throw new RegistryException($"{where}: unsupported manifest schema {manifest.Schema}.");
        if (!string.Equals(manifest.Id, source.Id, StringComparison.Ordinal))
            throw new RegistryException($"{where}: manifest id '{manifest.Id}' but sources.json binds this repo to '{source.Id}'.");

        RequireText(manifest.Name, "name", where);
        RequireText(manifest.Description, "description", where);
        RequireText(manifest.Author, "author", where);
        if (manifest.Homepage is not null && !IsHttpUrl(manifest.Homepage))
            throw new RegistryException($"{where}: homepage must be an http(s) URL.");

        ValidateVersion(manifest.Release, source, release, where);
    }

    private static void ValidateVersion(PluginVersion version, PluginSource source, PublishedRelease release, string where)
    {
        Formats.Require(Formats.Version, version.Version, $"{where}: version");
        Formats.Require(Formats.Version, version.MinModSystemVersion, $"{where}: minModSystemVersion");
        Formats.Require(Formats.Date, version.Date, $"{where}: date");
        Formats.Require(Formats.DllFileName, version.Dll, $"{where}: dll");
        Formats.Require(Formats.Sha256, version.Sha256, $"{where}: sha256");
        Formats.Require(Formats.CommitSha, version.SourceCommit, $"{where}: sourceCommit");
        if (version.MaxModSystemVersion is not null)
            Formats.Require(Formats.Version, version.MaxModSystemVersion, $"{where}: maxModSystemVersion");

        var tag = $"v{version.Version}";
        RequireEqual(release.Tag, tag, "release tag", where);
        RequireEqual(version.SourceTag, tag, "sourceTag", where);
        RequireEqual(version.SourceRepository, $"https://github.com/{source.Repository}.git", "sourceRepository", where);

        var expectedUrl = $"https://github.com/{source.Repository}/releases/download/{tag}/{version.Dll}";
        RequireEqual(version.DllUrl, expectedUrl, "dllUrl", where);
        if (!release.Assets.TryGetValue(version.Dll, out var asset) || !UrlEquals(asset, expectedUrl))
            throw new RegistryException($"{where}: the release has no asset '{version.Dll}' at {expectedUrl}.");
    }

    private static void RequireText(string? value, string field, string where)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new RegistryException($"{where}: {field} is required.");
    }

    // GitHub treats owner/repo case-insensitively, so URLs are compared the same way.
    private static void RequireEqual(string actual, string expected, string field, string where)
    {
        if (!UrlEquals(actual, expected))
            throw new RegistryException($"{where}: {field} is '{actual}', expected '{expected}'.");
    }

    private static bool UrlEquals(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool UrlEquals(Uri actual, string expected) => UrlEquals(actual.AbsoluteUri, expected);

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
