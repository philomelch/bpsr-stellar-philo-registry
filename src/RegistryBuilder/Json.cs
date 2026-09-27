using System.Text.Json;
using System.Text.Json.Serialization;

namespace RegistryBuilder;

internal static class Json
{
    /// <summary>camelCase per manifest-standard.md. Unknown fields are rejected, so an unexpected
    /// or tampered manifest fails loudly instead of being partly read.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };
}
