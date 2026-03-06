using System.Text;
using System.Text.Json;
using FluentAssertions;
using Oras.Credentials;
using Xunit;

namespace Oras.Tests.Credentials;

/// <summary>
/// Tests for DockerConfigStore — config loading/saving, credential round-trips,
/// base64 encoding, registry aggregation, and graceful error handling.
/// All tests use temp directories to avoid touching the real Docker config.
/// </summary>
public sealed class DockerConfigStoreTests : IDisposable
{
    private readonly string _tempDir;

    public DockerConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "oras-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string ConfigPath => Path.Combine(_tempDir, "config.json");

    private DockerConfigStore CreateStore() => new(ConfigPath);

    #region LoadAsync

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsEmptyConfig()
    {
        var store = CreateStore();

        var config = await store.LoadAsync();

        config.Should().NotBeNull();
        config.Auths.Should().BeEmpty();
        config.CredsStore.Should().BeNull();
        config.CredHelpers.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidConfig_DeserializesCorrectly()
    {
        var json = """
        {
            "auths": {
                "registry.example.com": {
                    "auth": "dXNlcjpwYXNz"
                }
            },
            "credsStore": "desktop",
            "credHelpers": {
                "gcr.io": "gcloud"
            }
        }
        """;
        await File.WriteAllTextAsync(ConfigPath, json);
        var store = CreateStore();

        var config = await store.LoadAsync();

        config.Auths.Should().ContainKey("registry.example.com");
        config.CredsStore.Should().Be("desktop");
        config.CredHelpers.Should().ContainKey("gcr.io");
        config.CredHelpers!["gcr.io"].Should().Be("gcloud");
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsEmptyConfig()
    {
        await File.WriteAllTextAsync(ConfigPath, "{{not valid json}}");
        var store = CreateStore();

        var config = await store.LoadAsync();

        config.Should().NotBeNull();
        config.Auths.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_ReturnsEmptyConfig()
    {
        await File.WriteAllTextAsync(ConfigPath, "");
        var store = CreateStore();

        var config = await store.LoadAsync();

        config.Should().NotBeNull();
        config.Auths.Should().BeEmpty();
    }

    #endregion

    #region SaveAsync

    [Fact]
    public async Task SaveAsync_CreatesDirectoryAndFile()
    {
        var nestedPath = Path.Combine(_tempDir, "nested", "dir", "config.json");
        var store = new DockerConfigStore(nestedPath);
        var config = new DockerConfig();
        config.Auths["test.io"] = new DockerAuth { Auth = "abc" };

        await store.SaveAsync(config);

        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_RoundTrip_PreservesData()
    {
        var store = CreateStore();
        var config = new DockerConfig
        {
            CredsStore = "osxkeychain",
            CredHelpers = new Dictionary<string, string> { ["ecr.aws"] = "ecr-login" }
        };
        config.Auths["docker.io"] = new DockerAuth { Auth = "dXNlcjpwYXNz" };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        loaded.CredsStore.Should().Be("osxkeychain");
        loaded.CredHelpers.Should().ContainKey("ecr.aws");
        loaded.Auths.Should().ContainKey("docker.io");
        loaded.Auths["docker.io"].Auth.Should().Be("dXNlcjpwYXNz");
    }

    #endregion

    #region StoreCredentialsAsync

    [Fact]
    public async Task StoreCredentialsAsync_NoHelper_WritesBase64Auth()
    {
        var store = CreateStore();

        await store.StoreCredentialsAsync("registry.test.io", "myuser", "mypass");

        var config = await store.LoadAsync();
        config.Auths.Should().ContainKey("registry.test.io");
        var authEntry = config.Auths["registry.test.io"];
        authEntry.Auth.Should().NotBeNullOrEmpty();

        // Verify base64 encoding
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authEntry.Auth!));
        decoded.Should().Be("myuser:mypass");
    }

    [Fact]
    public async Task StoreCredentialsAsync_OverwritesExistingAuth()
    {
        var store = CreateStore();
        await store.StoreCredentialsAsync("registry.test.io", "user1", "pass1");
        await store.StoreCredentialsAsync("registry.test.io", "user2", "pass2");

        var config = await store.LoadAsync();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(config.Auths["registry.test.io"].Auth!));
        decoded.Should().Be("user2:pass2");
    }

    #endregion

    #region GetCredentialsAsync

    [Fact]
    public async Task GetCredentialsAsync_Base64Auth_DecodesCorrectly()
    {
        var store = CreateStore();
        await store.StoreCredentialsAsync("myregistry.io", "admin", "secret123");

        var creds = await store.GetCredentialsAsync("myregistry.io");

        creds.Should().NotBeNull();
        creds!.Value.Username.Should().Be("admin");
        creds!.Value.Password.Should().Be("secret123");
    }

    [Fact]
    public async Task GetCredentialsAsync_UsernamePassword_ReturnsDirectly()
    {
        // Write config with explicit username/password fields
        var config = new DockerConfig();
        config.Auths["direct.io"] = new DockerAuth { Username = "directuser", Password = "directpass" };
        var store = CreateStore();
        await store.SaveAsync(config);

        var creds = await store.GetCredentialsAsync("direct.io");

        creds.Should().NotBeNull();
        creds!.Value.Username.Should().Be("directuser");
        creds!.Value.Password.Should().Be("directpass");
    }

    [Fact]
    public async Task GetCredentialsAsync_UnknownRegistry_ReturnsNull()
    {
        var store = CreateStore();

        var creds = await store.GetCredentialsAsync("unknown.io");

        creds.Should().BeNull();
    }

    [Fact]
    public async Task GetCredentialsAsync_InvalidBase64_ReturnsNull()
    {
        var config = new DockerConfig();
        config.Auths["bad.io"] = new DockerAuth { Auth = "!!!not-base64!!!" };
        var store = CreateStore();
        await store.SaveAsync(config);

        var creds = await store.GetCredentialsAsync("bad.io");

        creds.Should().BeNull();
    }

    #endregion

    #region RemoveCredentialsAsync

    [Fact]
    public async Task RemoveCredentialsAsync_ExistingAuth_RemovesEntry()
    {
        var store = CreateStore();
        await store.StoreCredentialsAsync("removeme.io", "user", "pass");

        await store.RemoveCredentialsAsync("removeme.io");

        var config = await store.LoadAsync();
        config.Auths.Should().NotContainKey("removeme.io");
    }

    [Fact]
    public async Task RemoveCredentialsAsync_NonexistentRegistry_DoesNotThrow()
    {
        var store = CreateStore();

        var act = () => store.RemoveCredentialsAsync("nonexistent.io");

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ListRegistriesAsync

    [Fact]
    public async Task ListRegistriesAsync_AuthsOnly_ReturnsAuthKeys()
    {
        var config = new DockerConfig();
        config.Auths["a.io"] = new DockerAuth { Auth = "abc" };
        config.Auths["b.io"] = new DockerAuth { Auth = "def" };
        var store = CreateStore();
        await store.SaveAsync(config);

        var registries = await store.ListRegistriesAsync();

        registries.Should().Contain("a.io");
        registries.Should().Contain("b.io");
        registries.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListRegistriesAsync_AuthsAndCredHelpers_AggregatesAndDeduplicates()
    {
        var config = new DockerConfig
        {
            CredHelpers = new Dictionary<string, string>
            {
                ["gcr.io"] = "gcloud",
                ["shared.io"] = "helper"  // will also be in auths
            }
        };
        config.Auths["shared.io"] = new DockerAuth { Auth = "abc" };
        config.Auths["docker.io"] = new DockerAuth { Auth = "def" };
        var store = CreateStore();
        await store.SaveAsync(config);

        var registries = await store.ListRegistriesAsync();

        registries.Should().Contain("gcr.io");
        registries.Should().Contain("shared.io");
        registries.Should().Contain("docker.io");
        // shared.io should appear only once despite being in both auths and credHelpers
        registries.Count(r => r.Equals("shared.io", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
    }

    [Fact]
    public async Task ListRegistriesAsync_EmptyConfig_ReturnsEmptyList()
    {
        var store = CreateStore();

        var registries = await store.ListRegistriesAsync();

        registries.Should().BeEmpty();
    }

    #endregion
}
