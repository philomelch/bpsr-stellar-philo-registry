using RegistryBuilder.Model;
using Xunit;

namespace RegistryBuilder.Tests;

public sealed class ManifestValidatorTests
{
    [Fact]
    public void Accepts_a_consistent_release()
    {
        ManifestValidator.Validate(Fixtures.Manifest("1.0.0"), Fixtures.Source, Fixtures.Release("1.0.0"));
    }

    [Fact]
    public void Rejects_an_id_this_repo_is_not_bound_to()
    {
        var manifest = Fixtures.Manifest("1.0.0") with { Id = "combatmeter" };
        Assert.Throws<RegistryException>(() => ManifestValidator.Validate(manifest, Fixtures.Source, Fixtures.Release("1.0.0")));
    }

    [Fact]
    public void Rejects_a_dll_url_outside_this_repo()
    {
        var manifest = Fixtures.Manifest("1.0.0");
        manifest = manifest with { Release = manifest.Release with { DllUrl = "https://evil.example/Stellar.PartyOverlay.dll" } };
        Assert.Throws<RegistryException>(() => ManifestValidator.Validate(manifest, Fixtures.Source, Fixtures.Release("1.0.0")));
    }

    [Fact]
    public void Rejects_a_manifest_found_under_a_different_tag()
    {
        Assert.Throws<RegistryException>(() =>
            ManifestValidator.Validate(Fixtures.Manifest("1.0.0"), Fixtures.Source, Fixtures.Release("1.0.1")));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0-beta")]
    [InlineData("v1.0.0")]
    public void Rejects_versions_the_launcher_cannot_compare(string version)
    {
        var manifest = Fixtures.Manifest("1.0.0");
        manifest = manifest with { Release = manifest.Release with { Version = version } };
        Assert.Throws<RegistryException>(() => ManifestValidator.Validate(manifest, Fixtures.Source, Fixtures.Release("1.0.0")));
    }

    [Fact]
    public void Rejects_a_path_in_the_dll_name()
    {
        var manifest = Fixtures.Manifest("1.0.0");
        manifest = manifest with { Release = manifest.Release with { Dll = "../evil.dll" } };
        Assert.Throws<RegistryException>(() => ManifestValidator.Validate(manifest, Fixtures.Source, Fixtures.Release("1.0.0")));
    }
}
