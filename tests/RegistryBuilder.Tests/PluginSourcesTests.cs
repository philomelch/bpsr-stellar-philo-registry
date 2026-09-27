using RegistryBuilder.Model;
using Xunit;

namespace RegistryBuilder.Tests;

public sealed class PluginSourcesTests
{
    [Fact]
    public void Parses_valid_sources()
    {
        var sources = PluginSources.Parse("""{ "plugins": [ { "id": "party-overlay", "repository": "me/StellarPartyOverlayPlugin" } ] }""");

        Assert.Equal("me/StellarPartyOverlayPlugin", Assert.Single(sources.Plugins).Repository);
    }

    [Fact]
    public void Parses_a_withdrawal()
    {
        var sources = PluginSources.Parse("""
            { "plugins": [ { "id": "party-overlay", "repository": "me/StellarPartyOverlayPlugin",
                             "withdrawn": [ { "version": "1.1.0", "reason": "crashes on login" } ] } ] }
            """);

        Assert.True(Assert.Single(sources.Plugins).IsWithdrawn("1.1.0"));
    }

    [Fact]
    public void Parses_detail_page_fields()
    {
        var sources = PluginSources.Parse("""
            { "publicUrl": "https://raw.githubusercontent.com/me/registry/main/",
              "plugins": [ { "id": "party-overlay", "repository": "me/StellarPartyOverlayPlugin", "guide": "guide.md",
                             "media": [ { "type": "image", "file": "media/overview.png", "caption": "Overview." } ] } ] }
            """);

        var source = Assert.Single(sources.Plugins);
        Assert.Equal("guide.md", source.Guide);
        Assert.Equal("media/overview.png", Assert.Single(source.Media!).File);
    }

    [Theory]
    [InlineData("""{ "plugins": [ { "id": "a", "repository": "me/x" }, { "id": "a", "repository": "me/y" } ] }""")]
    [InlineData("""{ "plugins": [ { "id": "Bad Id", "repository": "me/x" } ] }""")]
    [InlineData("""{ "plugins": [ { "id": "a", "repository": "https://github.com/me/x" } ] }""")]
    [InlineData("""{ "plugins": [ { "id": "a", "repository": "me/x", "withdrawn": [ { "version": "1.1.0", "reason": "" } ] } ] }""")]
    [InlineData("""{ "plugins": [ { "id": "a", "repository": "me/x", "withdrawn": [ { "version": "latest", "reason": "x" } ] } ] }""")]
    public void Rejects_invalid_sources(string json)
    {
        Assert.Throws<RegistryException>(() => PluginSources.Parse(json));
    }
}
