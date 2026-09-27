using System;

namespace RegistryBuilder;

/// <summary>A problem that must stop the update (invalid input, tampering, immutability breach).
/// Reported as a plain message; plugins.json is left untouched.</summary>
internal sealed class RegistryException : Exception
{
    public RegistryException(string message) : base(message) { }
}
