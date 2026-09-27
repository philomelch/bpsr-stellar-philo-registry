using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RegistryBuilder.GitHub;
using RegistryBuilder.Model;

namespace RegistryBuilder.Tests;

/// <summary>Builds consistent, valid releases; each test then breaks exactly one thing.</summary>
internal static class Fixtures
{
    public const string Repository = "me/StellarPartyOverlayPlugin";
    public const string Dll = "Stellar.PartyOverlay.dll";
    public static readonly PluginSource Source = new("party-overlay", Repository);

    public static byte[] DllBytes(string version) => Encoding.UTF8.GetBytes($"dll {version}");

    public static string Sha256Of(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static ReleaseManifest Manifest(string version, string? name = null, string? sha256 = null) => new(
        Schema: 1,
        Id: Source.Id,
        Name: name ?? "Party Overlay",
        Description: "Shows your party.",
        Author: "me",
        Tags: new[] { "party" },
        Homepage: null,
        Release: new PluginVersion(
            Version: version,
            Date: "2026-09-18",
            Dll: Dll,
            DllUrl: DllUrl(version),
            Sha256: sha256 ?? Sha256Of(DllBytes(version)),
            MinModSystemVersion: "2.8.1",
            MaxModSystemVersion: null,
            SourceRepository: $"https://github.com/{Repository}.git",
            SourceCommit: new string('a', 40),
            SourceTag: $"v{version}",
            Changelog: new Changelog(new[] { "Something." }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())));

    public static PublishedRelease Release(string version) => new($"v{version}", new Dictionary<string, Uri>
    {
        [Dll] = new Uri(DllUrl(version)),
        ["manifest.json"] = new Uri($"https://github.com/{Repository}/releases/download/v{version}/manifest.json"),
    });

    private static string DllUrl(string version) => $"https://github.com/{Repository}/releases/download/v{version}/{Dll}";
}

/// <summary>In-memory GitHub: releases per repo, bytes per asset URL.</summary>
internal sealed class FakeReleaseFeed : IReleaseFeed
{
    private readonly Dictionary<string, List<PublishedRelease>> _releases = new(StringComparer.Ordinal);
    private readonly Dictionary<Uri, byte[]> _assets = new();

    public List<Uri> Downloads { get; } = new();

    public FakeReleaseFeed Publish(ReleaseManifest manifest, byte[]? dll = null)
    {
        var release = Fixtures.Release(manifest.Release.Version);
        if (!_releases.TryGetValue(Fixtures.Repository, out var list)) _releases[Fixtures.Repository] = list = new();
        list.Add(release);
        _assets[release.Assets["manifest.json"]] = JsonSerializer.SerializeToUtf8Bytes(manifest, Json.Options);
        _assets[release.Assets[Fixtures.Dll]] = dll ?? Fixtures.DllBytes(manifest.Release.Version);
        return this;
    }

    public Task<IReadOnlyList<PublishedRelease>> ListReleasesAsync(string repository, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PublishedRelease>>(_releases.TryGetValue(repository, out var list) ? list.ToList() : new());

    public Task<byte[]> DownloadAsync(Uri asset, long maxBytes, CancellationToken ct)
    {
        Downloads.Add(asset);
        return Task.FromResult(_assets[asset]);
    }
}
