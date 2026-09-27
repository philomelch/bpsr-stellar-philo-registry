using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using RegistryBuilder;
using RegistryBuilder.GitHub;
using RegistryBuilder.Model;

// Usage: RegistryBuilder <sources.json> <plugins.json>
// Reads GITHUB_TOKEN from the environment (optional; raises the API rate limit).
// Exit 0 = plugins.json is up to date (rewritten only if something changed); 1 = update refused.
if (args.Length != 2)
{
    Console.Error.WriteLine("usage: RegistryBuilder <sources.json> <plugins.json>");
    return 2;
}

var (sourcesPath, registryPath) = (args[0], args[1]);
using var api = CreateApiClient(Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
using var downloads = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
downloads.DefaultRequestHeaders.UserAgent.ParseAdd("stellar-registry-builder");

try
{
    var sources = PluginSources.Parse(File.ReadAllText(sourcesPath));
    var presentation = new PresentationResolver(Path.GetDirectoryName(Path.GetFullPath(sourcesPath))!, sources.PublicUrl);
    var current = File.Exists(registryPath)
        ? JsonSerializer.Deserialize<PluginsFile>(File.ReadAllText(registryPath), Json.Options) ?? PluginsFile.Empty
        : PluginsFile.Empty;
    var officialIds = await OfficialRegistry.FetchIdsAsync(downloads, CancellationToken.None);

    var updater = new RegistryUpdater(new GitHubReleaseFeed(api, downloads), presentation.Resolve, Console.WriteLine);
    var updated = await updater.UpdateAsync(sources, current, officialIds, CancellationToken.None);

    // LF on every OS, so a run on Windows produces the same bytes as CI and doesn't commit noise.
    var json = JsonSerializer.Serialize(updated, Json.Options).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    var unchanged = File.Exists(registryPath) && File.ReadAllText(registryPath) == json;
    if (!unchanged) File.WriteAllText(registryPath, json);
    Console.WriteLine(unchanged ? "plugins.json is already up to date." : "::notice::plugins.json updated.");
    return 0;
}
catch (Exception problem) when (problem is RegistryException or JsonException or HttpRequestException)
{
    Console.Error.WriteLine($"::error::{problem.Message}");
    return 1;
}

static HttpClient CreateApiClient(string? token)
{
    var client = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
    client.DefaultRequestHeaders.UserAgent.ParseAdd("stellar-registry-builder");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    if (!string.IsNullOrEmpty(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
}
