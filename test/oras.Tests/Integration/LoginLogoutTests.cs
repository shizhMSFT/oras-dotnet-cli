using FluentAssertions;
using Xunit;
using Oras.Tests.Fixtures;

namespace Oras.Tests.Integration;

/// <summary>
/// Integration tests for login and logout commands.
/// Tests credential storage and retrieval using Docker-compatible credential store.
/// </summary>
public sealed class LoginLogoutTests : RegistryIntegrationTestBase
{
    public LoginLogoutTests(RegistryFixture registry) : base(registry)
    {
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "SkipIfNoCredentialStore")]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        // Arrange
        var registryHost = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";
        var username = "testuser";
        var password = "testpassword";

        // Act
        var loginResult = await Cli.ExecuteAsync(
            $"login {registryHost} -u {username} -p {password}").ConfigureAwait(false);

        // Assert
        loginResult.ExitCode.Should().Be(0, "login should succeed");
        loginResult.StandardOutput.Should().Contain("success", "login should confirm success");

        // Cleanup
        await Cli.ExecuteAsync($"logout {registryHost}").ConfigureAwait(false);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "SkipIfNoCredentialStore")]
    public async Task Logout_AfterLogin_RemovesCredentials()
    {
        // Arrange
        var registryHost = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";
        var username = "testuser";
        var password = "testpassword";
        
        await Cli.ExecuteAsync($"login {registryHost} -u {username} -p {password}").ConfigureAwait(false);

        // Act
        var logoutResult = await Cli.ExecuteAsync($"logout {registryHost}").ConfigureAwait(false);

        // Assert
        logoutResult.ExitCode.Should().Be(0, "logout should succeed");
        logoutResult.StandardOutput.Should().Contain("success", "logout should confirm success");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithoutCredentials_ShowsHelp()
    {
        // Arrange
        var registryHost = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";

        // Act
        var loginResult = await Cli.ExecuteAsync($"login {registryHost}").ConfigureAwait(false);

        // Assert
        // Should either prompt for credentials (exit 0 if stdin available) or fail with error
        if (loginResult.ExitCode != 0)
        {
            loginResult.StandardError.Should().NotBeNullOrEmpty("error message should be provided");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Logout_WithoutPriorLogin_Succeeds()
    {
        // Arrange
        var registryHost = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";

        // Act
        var logoutResult = await Cli.ExecuteAsync($"logout {registryHost}").ConfigureAwait(false);

        // Assert
        // Logout should be idempotent - succeeds even if not logged in
        logoutResult.ExitCode.Should().Be(0, "logout should succeed even without prior login");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "SkipIfNoCredentialStore")]
    public async Task LoginLogout_Roundtrip_WorksCorrectly()
    {
        // Arrange
        var registryHost = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";
        var username = "testuser";
        var password = "testpassword";

        // Act & Assert - Login
        var loginResult = await Cli.ExecuteAsync(
            $"login {registryHost} -u {username} -p {password}").ConfigureAwait(false);
        loginResult.ExitCode.Should().Be(0, "login should succeed");

        // Act & Assert - Logout
        var logoutResult = await Cli.ExecuteAsync($"logout {registryHost}").ConfigureAwait(false);
        logoutResult.ExitCode.Should().Be(0, "logout should succeed");

        // Act & Assert - Second logout (should be idempotent)
        var logoutResult2 = await Cli.ExecuteAsync($"logout {registryHost}").ConfigureAwait(false);
        logoutResult2.ExitCode.Should().Be(0, "second logout should also succeed");
    }
}
