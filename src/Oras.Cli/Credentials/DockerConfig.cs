using System.Text.Json.Serialization;

namespace Oras.Credentials;

/// <summary>
/// Docker config.json file structure.
/// </summary>
internal class DockerConfig
{
    [JsonPropertyName("auths")]
    public Dictionary<string, DockerAuth> Auths { get; set; } = new();

    [JsonPropertyName("credsStore")]
    public string? CredsStore { get; set; }

    [JsonPropertyName("credHelpers")]
    public Dictionary<string, string>? CredHelpers { get; set; }
}

/// <summary>
/// Docker authentication entry.
/// </summary>
internal class DockerAuth
{
    [JsonPropertyName("auth")]
    public string? Auth { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("identitytoken")]
    public string? IdentityToken { get; set; }
}
