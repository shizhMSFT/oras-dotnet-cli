using System.Text.Json.Serialization;

namespace Oras.Credentials;

/// <summary>
/// Source-generated JSON serializer context for credential types.
/// Required for AOT compatibility — reflection-based serialization
/// is stripped during native AOT compilation.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true)]
[JsonSerializable(typeof(DockerConfig))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(CredentialHelperResponse))]
[JsonSerializable(typeof(CredentialHelperInput))]
internal partial class CredentialJsonContext : JsonSerializerContext;

/// <summary>
/// Docker credential helper response from the <c>get</c> action.
/// </summary>
internal class CredentialHelperResponse
{
    [JsonPropertyName("Username")]
    public string? Username { get; set; }

    [JsonPropertyName("Secret")]
    public string? Secret { get; set; }
}

/// <summary>
/// Docker credential helper input for the <c>store</c> action.
/// </summary>
internal class CredentialHelperInput
{
    [JsonPropertyName("ServerURL")]
    public string ServerURL { get; set; } = string.Empty;

    [JsonPropertyName("Username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("Secret")]
    public string Secret { get; set; } = string.Empty;
}
