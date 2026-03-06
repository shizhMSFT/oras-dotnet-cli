using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oras.Output;

/// <summary>
/// Source-generated JSON serializer context for CLI output types.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StatusResult))]
[JsonSerializable(typeof(ErrorResult))]
[JsonSerializable(typeof(CopyResult))]
[JsonSerializable(typeof(BackupResult))]
[JsonSerializable(typeof(RestoreResult))]
[JsonSerializable(typeof(DescriptorResult))]
[JsonSerializable(typeof(TableResult))]
[JsonSerializable(typeof(TreeNodeResult))]
[JsonSerializable(typeof(JsonDocument))]
internal partial class OutputJsonContext : JsonSerializerContext;
