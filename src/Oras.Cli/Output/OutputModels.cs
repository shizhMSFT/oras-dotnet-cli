using System.Collections.Generic;

namespace Oras.Output;

/// <summary>
/// Structured output for status messages.
/// </summary>
internal sealed record StatusResult(string Status, string Message);

/// <summary>
/// Structured output for errors.
/// </summary>
internal sealed record ErrorResult(string Status, string Error, string? Recommendation);

/// <summary>
/// Structured output for copy command results.
/// </summary>
internal sealed record CopyResult(
    string Source,
    string Destination,
    bool Recursive,
    int Concurrency,
    string Platform,
    string Status);

/// <summary>
/// Structured output for backup command results.
/// </summary>
internal sealed record BackupResult(
    string Reference,
    string Output,
    int Layers,
    string TotalSize,
    bool Recursive,
    string Platform,
    string Status);

/// <summary>
/// Structured output for restore command results.
/// </summary>
internal sealed record RestoreResult(
    string Source,
    string Destination,
    bool Recursive,
    int Concurrency,
    string Status);

/// <summary>
/// Structured output for descriptors.
/// </summary>
internal sealed record DescriptorResult(
    string MediaType,
    string Digest,
    long Size,
    Dictionary<string, string>? Annotations);

/// <summary>
/// Structured output for table results.
/// </summary>
internal sealed record TableResult(Dictionary<string, string>[] Items);

/// <summary>
/// Structured output for tree nodes.
/// </summary>
internal sealed record TreeNodeResult(
    string Label,
    Dictionary<string, string> Metadata,
    TreeNodeResult[] Children);

/// <summary>
/// Structured output for push command results.
/// </summary>
internal sealed record PushResult(string Reference, string Digest, string MediaType, long Size);

/// <summary>
/// Structured output for pull command results.
/// </summary>
internal sealed record PullResult(string Reference, int FileCount, string[] Files);

/// <summary>
/// Structured output for attach command results.
/// </summary>
internal sealed record AttachResult(string Reference, string Subject, string Digest, string ArtifactType);

/// <summary>
/// Structured output for tag command results.
/// </summary>
internal sealed record TagResult(string Source, string[] Tags);

/// <summary>
/// Structured output for list command results.
/// </summary>
internal sealed record ListResult(string[] Items);

/// <summary>
/// Structured output for discover command results.
/// </summary>
internal sealed record DiscoverResult(string Reference, DescriptorResult[] Referrers);

/// <summary>
/// Structured output for delete command results.
/// </summary>
internal sealed record DeleteResult(string Reference, string Status);
