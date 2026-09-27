using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryBuilder;

/// <summary>Reads the ids of the official curated registry, so our registry never shadows one.</summary>
internal static class OfficialRegistry
{
    public static readonly Uri StableUrl = new("https://cdn.revette.io/plugins.json");
    public static readonly Uri TestingUrl = new("https://cdn.revette.io/plugins-testing.json");

    /// <summary>Union of the stable and testing ids. Fails closed: if either can't be read, the
    /// update stops rather than risk publishing a colliding id.</summary>
    public static async Task<IReadOnlySet<string>> FetchIdsAsync(HttpClient http, CancellationToken ct)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var url in new[] { StableUrl, TestingUrl })
        {
            var registry = await http.GetFromJsonAsync<IdsOnly>(url, ct).ConfigureAwait(false)
                ?? throw new RegistryException($"Official registry {url} is empty.");
            ids.UnionWith(registry.Plugins.Select(plugin => plugin.Id));
        }
        return ids;
    }

    private sealed record IdsOnly(IReadOnlyList<IdOnly> Plugins);

    private sealed record IdOnly(string Id);
}
