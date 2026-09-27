using System.Text.RegularExpressions;

namespace RegistryBuilder;

/// <summary>Formats the launcher relies on. It compares versions with System.Version, so only
/// numeric MAJOR.MINOR.PATCH is accepted.</summary>
internal static class Formats
{
    public static readonly Regex Version = new(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);
    public static readonly Regex PluginId = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    public static readonly Regex Repository = new(@"^[A-Za-z0-9-]+/[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant);
    public static readonly Regex CommitSha = new(@"^[0-9a-f]{40}$", RegexOptions.CultureInvariant);
    public static readonly Regex Sha256 = new(@"^[0-9a-f]{64}$", RegexOptions.CultureInvariant);
    public static readonly Regex DllFileName = new(@"^[A-Za-z0-9._-]+\.dll$", RegexOptions.CultureInvariant);
    // A relative path of plain, URL-safe segments. No segment may start with '.', which rules out
    // "." and ".." (escaping the plugin's folder) and hidden files.
    public static readonly Regex RelativeFile = new(@"^[A-Za-z0-9_-][A-Za-z0-9._-]*(/[A-Za-z0-9_-][A-Za-z0-9._-]*)*$", RegexOptions.CultureInvariant);
    public static readonly Regex Date =new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant);

    public static string Require(Regex format, string? value, string what)
    {
        if (value is null || !format.IsMatch(value))
            throw new RegistryException($"{what} '{value}' is not in the expected format ({format}).");
        return value;
    }
}
