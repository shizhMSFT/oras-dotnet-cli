namespace Oras.Commands;

/// <summary>
/// Shared utilities for parsing and normalizing OCI references.
/// </summary>
internal static class ReferenceHelper
{
    /// <summary>
    /// Normalizes a registry address by stripping protocol prefixes,
    /// trimming trailing slashes, and handling Docker Hub special case.
    /// </summary>
    public static string NormalizeRegistry(string registry)
    {
        // Remove protocol if present
        registry = registry.Replace("https://", "").Replace("http://", "");

        // Remove trailing slash
        registry = registry.TrimEnd('/');

        // Handle Docker Hub special case
        if (registry.Equals("docker.io", StringComparison.OrdinalIgnoreCase))
        {
            return "registry-1.docker.io";
        }

        return registry;
    }

    /// <summary>
    /// Parses a full OCI reference into its components: registry, repository, tag, and digest.
    /// </summary>
    public static (string registry, string repository, string? tag, string? digest) ParseReference(string reference)
    {
        var parts = reference.Split('/', 2);
        if (parts.Length < 2)
        {
            throw new OrasUsageException(
                $"Invalid reference format: {reference}",
                "Reference must be in format: registry/repository[:tag|@digest]");
        }

        var registry = parts[0];
        var rest = parts[1];

        string? tag = null;
        string? digest = null;
        string repository;

        if (rest.Contains('@'))
        {
            var digestParts = rest.Split('@', 2);
            repository = digestParts[0];
            digest = digestParts[1];
        }
        else if (rest.Contains(':'))
        {
            var tagParts = rest.Split(':', 2);
            repository = tagParts[0];
            tag = tagParts[1];
        }
        else
        {
            repository = rest;
        }

        return (registry, repository, tag, digest);
    }

    /// <summary>
    /// Extracts the tag portion from a reference string, defaulting to "latest" if none is specified.
    /// Returns null when the reference uses a digest instead of a tag.
    /// </summary>
    public static string? ExtractTag(string reference)
    {
        var colonIndex = reference.LastIndexOf(':');
        var slashIndex = reference.LastIndexOf('/');
        var atIndex = reference.IndexOf('@');

        if (atIndex > 0)
        {
            return null; // Has digest, no tag
        }

        if (colonIndex > slashIndex && colonIndex >= 0)
        {
            return reference[(colonIndex + 1)..];
        }

        return "latest"; // Default tag
    }

    /// <summary>
    /// Extracts the digest portion from a reference string, or null if none is present.
    /// </summary>
    public static string? ExtractDigest(string reference)
    {
        var atIndex = reference.IndexOf('@');
        if (atIndex > 0)
        {
            return reference[(atIndex + 1)..];
        }

        return null;
    }
}
