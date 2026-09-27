using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegistryBuilder.Model;
using Xunit;

namespace RegistryBuilder.Tests;

public sealed class RegistryUpdaterTests
{
    private static readonly PluginSources Sources = new(new[] { Fixtures.Source });
    private static readonly IReadOnlySet<string> NoOfficialIds = new HashSet<string>();

    [Fact]
    public async Task Publishes_a_verified_release()
    {
        var feed = new FakeReleaseFeed().Publish(Fixtures.Manifest("1.0.0"));

        var updated = await Update(feed, PluginsFile.Empty);

        Assert.Equal("1.0.0", updated.Plugins.Single().Versions.Single().Version);
    }

    [Fact]
    public async Task Refuses_a_dll_that_does_not_match_its_manifest()
    {
        var feed = new FakeReleaseFeed().Publish(Fixtures.Manifest("1.0.0"), dll: new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<RegistryException>(() => Update(feed, PluginsFile.Empty));
    }

    [Fact]
    public async Task Does_not_redownload_dlls_already_published()
    {
        var feed = new FakeReleaseFeed().Publish(Fixtures.Manifest("1.0.0"));
        var published = await Update(feed, PluginsFile.Empty);
        feed.Downloads.Clear();

        await Update(feed, published);

        Assert.DoesNotContain(feed.Downloads, url => url.AbsolutePath.EndsWith(".dll", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Skips_withdrawn_versions_without_downloading_them()
    {
        var feed = new FakeReleaseFeed().Publish(Fixtures.Manifest("1.0.0")).Publish(Fixtures.Manifest("1.1.0"));
        var source = Fixtures.Source with { Withdrawn = new[] { new WithdrawnVersion("1.1.0", "crashes on login") } };

        var updated = await new RegistryUpdater(feed, _ => PluginPresentation.None, _ => { })
            .UpdateAsync(new PluginSources(new[] { source }), PluginsFile.Empty, NoOfficialIds, CancellationToken.None);

        Assert.Equal("1.0.0", updated.Plugins.Single().Versions.Single().Version);
        Assert.DoesNotContain(feed.Downloads, url => url.AbsolutePath.EndsWith("/v1.1.0/" + Fixtures.Dll, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Withdrawing_a_release_with_a_broken_manifest_unblocks_the_update()
    {
        var broken = Fixtures.Manifest("1.1.0") with { Id = "someone-else" };
        var feed = new FakeReleaseFeed().Publish(Fixtures.Manifest("1.0.0")).Publish(broken);
        var source = Fixtures.Source with { Withdrawn = new[] { new WithdrawnVersion("1.1.0", "broken manifest") } };
        await Assert.ThrowsAsync<RegistryException>(() => Update(feed, PluginsFile.Empty));

        var updated = await new RegistryUpdater(feed, _ => PluginPresentation.None, _ => { })
            .UpdateAsync(new PluginSources(new[] { source }), PluginsFile.Empty, NoOfficialIds, CancellationToken.None);

        Assert.Equal("1.0.0", updated.Plugins.Single().Versions.Single().Version);
    }

    [Fact]
    public async Task Refuses_an_id_used_by_the_official_registry()
    {
        var feed = new FakeReleaseFeed().Publish(Fixtures.Manifest("1.0.0"));
        var official = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Party-Overlay" };

        await Assert.ThrowsAsync<RegistryException>(() =>
            new RegistryUpdater(feed, _ => PluginPresentation.None, _ => { }).UpdateAsync(Sources, PluginsFile.Empty, official, CancellationToken.None));
    }

    private static Task<PluginsFile> Update(FakeReleaseFeed feed, PluginsFile current) =>
        new RegistryUpdater(feed, _ => PluginPresentation.None, _ => { }).UpdateAsync(Sources, current, NoOfficialIds, CancellationToken.None);
}
