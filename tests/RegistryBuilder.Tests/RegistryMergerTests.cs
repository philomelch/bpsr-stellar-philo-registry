using System.Linq;
using RegistryBuilder.Model;
using Xunit;

namespace RegistryBuilder.Tests;

public sealed class RegistryMergerTests
{
    [Fact]
    public void Adds_new_versions_newest_first()
    {
        var merged = Merge(PluginsFile.Empty, Fixtures.Manifest("1.2.0"), Fixtures.Manifest("1.10.0"), Fixtures.Manifest("1.9.0"));

        Assert.Equal(new[] { "1.10.0", "1.9.0", "1.2.0" }, merged.Plugins.Single().Versions.Select(v => v.Version));
    }

    [Fact]
    public void Keeps_published_history_when_a_release_disappears()
    {
        var published = Merge(PluginsFile.Empty, Fixtures.Manifest("1.0.0"), Fixtures.Manifest("1.1.0"));

        var merged = Merge(published, Fixtures.Manifest("1.1.0"));

        Assert.Equal(new[] { "1.1.0", "1.0.0" }, merged.Plugins.Single().Versions.Select(v => v.Version));
    }

    [Fact]
    public void Refuses_to_change_a_published_version()
    {
        var published = Merge(PluginsFile.Empty, Fixtures.Manifest("1.0.0"));
        var tampered = Fixtures.Manifest("1.0.0", sha256: new string('0', 64));

        Assert.Throws<RegistryException>(() => Merge(published, tampered));
    }

    [Fact]
    public void Plugin_fields_follow_the_newest_release()
    {
        var merged = Merge(PluginsFile.Empty, Fixtures.Manifest("2.0.0", name: "Party Overlay 2"), Fixtures.Manifest("1.0.0", name: "Old"));

        Assert.Equal("Party Overlay 2", merged.Plugins.Single().Name);
    }

    [Fact]
    public void Detail_page_fields_follow_sources_json()
    {
        var presentation = new PluginPresentation(null, "https://x.test/plugins/party-overlay/guide.md", null);
        var published = Merge(PluginsFile.Empty, Fixtures.Manifest("1.0.0"));

        var withGuide = RegistryMerger.Merge(published, new[] { new DiscoveredPlugin(Fixtures.Source, new[] { Fixtures.Manifest("1.0.0") }, presentation) });
        var guideRemoved = Merge(withGuide, Fixtures.Manifest("1.0.0"));

        Assert.Equal(presentation.GuideUrl, withGuide.Plugins.Single().GuideUrl);
        Assert.Null(guideRemoved.Plugins.Single().GuideUrl);
    }

    [Fact]
    public void Omits_listed_plugins_without_releases()
    {
        Assert.Empty(Merge(PluginsFile.Empty).Plugins);
    }

    [Fact]
    public void Withdrawing_a_version_rolls_the_newest_back_to_the_previous_one()
    {
        var published = Merge(PluginsFile.Empty, Fixtures.Manifest("1.0.0"), Fixtures.Manifest("1.1.0"));
        var withdrawn = Fixtures.Source with { Withdrawn = new[] { new WithdrawnVersion("1.1.0", "crashes on login") } };

        var merged = RegistryMerger.Merge(published, new[]
        {
            new DiscoveredPlugin(withdrawn, new[] { Fixtures.Manifest("1.0.0"), Fixtures.Manifest("1.1.0") }),
        });

        Assert.Equal(new[] { "1.0.0" }, merged.Plugins.Single().Versions.Select(v => v.Version));
    }

    [Fact]
    public void Withdrawing_every_version_delists_the_plugin()
    {
        var published = Merge(PluginsFile.Empty, Fixtures.Manifest("1.0.0"));
        var withdrawn = Fixtures.Source with { Withdrawn = new[] { new WithdrawnVersion("1.0.0", "broken") } };

        var merged = RegistryMerger.Merge(published, new[] { new DiscoveredPlugin(withdrawn, new[] { Fixtures.Manifest("1.0.0") }) });

        Assert.Empty(merged.Plugins);
    }

    private static PluginsFile Merge(PluginsFile current, params ReleaseManifest[] manifests) =>
        RegistryMerger.Merge(current, new[] { new DiscoveredPlugin(Fixtures.Source, manifests) });
}
