using System.Text;
using FluentAssertions;
using Oras.Credentials;
using Oras.Services;
using Xunit;

namespace Oras.Tests.Services;

/// <summary>
/// Tests for CredentialService — delegation to DockerConfigStore, round-trip
/// credential storage, and best-effort removal behavior.
/// Uses real DockerConfigStore with temp files (DockerConfigStore is a concrete class,
/// not interfaced, so we test through it).
/// </summary>
public sealed class CredentialServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly DockerConfigStore _configStore;
    private readonly CredentialService _service;

    public CredentialServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "oras-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
        _configStore = new DockerConfigStore(_configPath);
        _service = new CredentialService(_configStore);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task StoreCredentialsAsync_DelegatesToConfigStore()
    {
        await _service.StoreCredentialsAsync("test.io", "user", "pass");

        // Verify by loading directly from the config store
        var config = await _configStore.LoadAsync();
        config.Auths.Should().ContainKey("test.io");
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(config.Auths["test.io"].Auth!));
        decoded.Should().Be("user:pass");
    }

    [Fact]
    public async Task GetCredentialsAsync_ReturnsStoredCredentials()
    {
        await _service.StoreCredentialsAsync("get-test.io", "admin", "secret");

        var creds = await _service.GetCredentialsAsync("get-test.io");

        creds.Should().NotBeNull();
        creds!.Value.Username.Should().Be("admin");
        creds!.Value.Password.Should().Be("secret");
    }

    [Fact]
    public async Task GetCredentialsAsync_UnknownRegistry_ReturnsNull()
    {
        var creds = await _service.GetCredentialsAsync("unknown.io");

        creds.Should().BeNull();
    }

    [Fact]
    public async Task RemoveCredentialsAsync_ExistingCredentials_RemovesThem()
    {
        await _service.StoreCredentialsAsync("remove-test.io", "user", "pass");
        await _service.RemoveCredentialsAsync("remove-test.io");

        var creds = await _service.GetCredentialsAsync("remove-test.io");
        creds.Should().BeNull();
    }

    [Fact]
    public async Task RemoveCredentialsAsync_NonexistentRegistry_DoesNotThrow()
    {
        // Best-effort removal should not throw even if registry doesn't exist
        var act = () => _service.RemoveCredentialsAsync("never-stored.io");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StoreAndRetrieve_MultipleRegistries_IndependentEntries()
    {
        await _service.StoreCredentialsAsync("reg1.io", "user1", "pass1");
        await _service.StoreCredentialsAsync("reg2.io", "user2", "pass2");

        var creds1 = await _service.GetCredentialsAsync("reg1.io");
        var creds2 = await _service.GetCredentialsAsync("reg2.io");

        creds1!.Value.Username.Should().Be("user1");
        creds2!.Value.Username.Should().Be("user2");
    }
}
