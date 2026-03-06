using OrasProject.Oras.Oci;

namespace Oras.Services;

/// <summary>
/// Service for pushing artifacts to OCI registries.
/// </summary>
public interface IPushService
{
    /// <summary>
    /// Push files to a registry reference.
    /// </summary>
    Task<Descriptor> PushAsync(
        string reference,
        IEnumerable<string> filePaths,
        string? artifactType = null,
        IDictionary<string, string>? annotations = null,
        string? configPath = null,
        int concurrency = 3,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
