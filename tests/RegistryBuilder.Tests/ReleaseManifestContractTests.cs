using System.Text.Json;
using RegistryBuilder.GitHub;
using RegistryBuilder.Model;
using Xunit;

namespace RegistryBuilder.Tests;

/// <summary>The plugin template's tools/ReleaseTool writes manifest.json; this registry reads it with
/// unknown fields disallowed. This is verbatim ReleaseTool output: if either side changes shape,
/// this test fails before a real release does. Update both sides (and this sample) together.</summary>
public sealed class ReleaseManifestContractTests
{
    private const string ReleaseToolOutput = """
        {
          "schema": 1,
          "id": "party-overlay",
          "name": "Party Overlay",
          "description": "Shows your party at a glance.",
          "author": "tester",
          "tags": [],
          "homepage": null,
          "release": {
            "version": "1.0.0",
            "date": "2026-09-18",
            "dll": "Stellar.PartyOverlay.dll",
            "dllUrl": "https://github.com/me/StellarPartyOverlayPlugin/releases/download/v1.0.0/Stellar.PartyOverlay.dll",
            "sha256": "2b1cc5f2e2e39c891cbb44479f8b591c8092e22aef864474b32a43eae6302fc8",
            "minModSystemVersion": "2.8.1",
            "maxModSystemVersion": null,
            "sourceRepository": "https://github.com/me/StellarPartyOverlayPlugin.git",
            "sourceCommit": "0123456789abcdef0123456789abcdef01234567",
            "sourceTag": "v1.0.0",
            "changelog": {
              "added": [ "Initial plugin scaffold." ],
              "changed": [],
              "fixed": [],
              "removed": []
            }
          }
        }
        """;

    [Fact]
    public void Registry_reads_and_accepts_release_tool_output()
    {
        var manifest = JsonSerializer.Deserialize<ReleaseManifest>(ReleaseToolOutput, Json.Options)!;

        ManifestValidator.Validate(manifest, Fixtures.Source, Fixtures.Release("1.0.0"));
        Assert.Equal("2.8.1", manifest.Release.MinModSystemVersion);
    }
}
