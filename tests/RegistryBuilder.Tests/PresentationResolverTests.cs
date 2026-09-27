using System;
using System.IO;
using System.Text.Json;
using RegistryBuilder.Model;
using Xunit;

namespace RegistryBuilder.Tests;

public sealed class PresentationResolverTests : IDisposable
{
    private const string PublicUrl = "https://raw.githubusercontent.com/me/registry/main/";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "registry-tests-" + Guid.NewGuid().ToString("N"));

    public PresentationResolverTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Publishes_files_from_the_plugins_folder_at_the_public_url()
    {
        AddFile("guide.md");
        AddFile("media/overview.png");
        var source = Fixtures.Source with
        {
            Guide = "guide.md",
            Media = new[] { new MediaSource("image", File: "media/overview.png", Caption: "Overview.") },
            Icon = "media/overview.png",
        };

        var presentation = Resolver().Resolve(source);

        Assert.Equal(PublicUrl + "plugins/party-overlay/guide.md", presentation.GuideUrl);
        Assert.Equal(new PluginMedia("image", PublicUrl + "plugins/party-overlay/media/overview.png", "Overview."), Assert.Single(presentation.Media!));
        Assert.Equal(PublicUrl + "plugins/party-overlay/media/overview.png", presentation.IconUrl);
    }

    [Fact]
    public void Passes_hosted_media_through()
    {
        var source = Fixtures.Source with
        {
            Media = new[] { new MediaSource("youtube", Url: "https://www.youtube.com/watch?v=abcdefghijk") },
        };

        var presentation = new PresentationResolver(_root, publicUrl: null).Resolve(source);

        Assert.Equal("https://www.youtube.com/watch?v=abcdefghijk", Assert.Single(presentation.Media!).Url);
    }

    [Fact]
    public void A_plugin_without_presentation_publishes_none()
    {
        var presentation = new PresentationResolver(_root, publicUrl: null).Resolve(Fixtures.Source);

        Assert.Equal(PluginPresentation.None, presentation);
    }

    [Theory]
    [InlineData("missing.md")]          // not there
    [InlineData("../other/guide.md")]   // escapes the plugin's folder
    [InlineData("guide.txt")]           // not markdown
    [InlineData("/guide.md")]           // absolute
    public void Refuses_a_bad_guide(string guide)
    {
        AddFile("guide.txt");
        Directory.CreateDirectory(Path.Combine(_root, "plugins", "other"));
        File.WriteAllText(Path.Combine(_root, "plugins", "other", "guide.md"), "# Other");

        Assert.Throws<RegistryException>(() => Resolver().Resolve(Fixtures.Source with { Guide = guide }));
    }

    [Fact]
    public void Refuses_a_guide_over_one_megabyte()
    {
        AddFile("guide.md", new string('a', 1024 * 1024 + 1));

        Assert.Throws<RegistryException>(() => Resolver().Resolve(Fixtures.Source with { Guide = "guide.md" }));
    }

    [Fact]
    public void Needs_a_public_url_to_publish_files()
    {
        AddFile("guide.md");

        Assert.Throws<RegistryException>(() => new PresentationResolver(_root, publicUrl: null).Resolve(Fixtures.Source with { Guide = "guide.md" }));
    }

    [Theory]
    [InlineData("image", null, null)]                                   // neither file nor url
    [InlineData("image", "media/overview.png", "https://x.test/a.png")] // both
    [InlineData("youtube", "media/overview.png", null)]                 // youtube must be a url
    [InlineData("image", null, "http://x.test/a.png")]                  // not https
    [InlineData("gif", "media/overview.png", null)]                     // unknown type
    public void Refuses_bad_media(string type, string? file, string? url)
    {
        AddFile("media/overview.png");
        var source = Fixtures.Source with { Media = new[] { new MediaSource(type, file, url) } };

        Assert.Throws<RegistryException>(() => Resolver().Resolve(source));
    }

    [Theory]
    [InlineData("http://raw.githubusercontent.com/me/registry/main/")]
    [InlineData("https://raw.githubusercontent.com/me/registry/main")]
    public void Refuses_a_bad_public_url(string publicUrl)
    {
        Assert.Throws<RegistryException>(() => new PresentationResolver(_root, publicUrl));
    }

    [Fact]
    public void Leaves_absent_fields_out_of_plugins_json()
    {
        var entry = new PluginEntry("party-overlay", "Party Overlay", "Shows your party.", "me", null, null, null, null, null,
            Array.Empty<PluginVersion>());

        var json = JsonSerializer.Serialize(entry, Json.Options);

        Assert.DoesNotContain("media", json, StringComparison.Ordinal);
        Assert.DoesNotContain("guideUrl", json, StringComparison.Ordinal);
        Assert.DoesNotContain("iconUrl", json, StringComparison.Ordinal);
    }

    private PresentationResolver Resolver() => new(_root, PublicUrl);

    private void AddFile(string path, string content = "x")
    {
        var full = Path.Combine(_root, "plugins", Fixtures.Source.Id, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }
}
