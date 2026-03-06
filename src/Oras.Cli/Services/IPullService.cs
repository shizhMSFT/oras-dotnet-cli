namespace Oras.Services;

/// <summary>
/// Service for pulling artifacts from OCI registries.
/// </summary>
internal interface IPullService
{
    /// <summary>
    /// Pull artifact layers to output directory.
    /// </summary>
    Task PullAsync(
        string reference,
        string outputDir,
        string? platform = null,
        bool keepOldFiles = false,
        int concurrency = 3,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
