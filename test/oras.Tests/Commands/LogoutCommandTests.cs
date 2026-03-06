using FluentAssertions;
using Oras.Tests.Helpers;
using Xunit;

namespace Oras.Tests.Commands;

/// <summary>
/// Unit tests for LogoutCommand.
/// </summary>
public sealed class LogoutCommandTests
{
    [Fact]
    public async Task Logout_WithoutArguments_ReturnsArgumentError()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await new CliRunner().ExecuteAsync("logout");

        // Assert
        result.ExitCode.Should().NotBe(0, "logout without registry should fail");
        result.StandardError.Should().Contain("Required argument missing", "should mention missing argument");
    }

    [Fact]
    public async Task Logout_WithRegistry_ReturnsSuccess()
    {
        // Arrange
        var args = "logout localhost:5000";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, "logout should succeed even without prior login");
        result.StandardOutput.Should().Contain("succeeded", "should confirm logout success");
    }

    [Fact]
    public async Task Logout_WithDockerIo_NormalizesToRegistry()
    {
        // Arrange
        var args = "logout docker.io";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, "logout should succeed");
        result.StandardOutput.Should().Contain("succeeded", "should confirm logout success");
    }

    [Fact]
    public async Task Logout_WithHttpsPrefix_RemovesPrefix()
    {
        // Arrange
        var args = "logout https://registry.example.com";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, "logout should succeed");
        result.StandardOutput.Should().Contain("registry.example.com", "should normalize registry address");
    }

    [Fact]
    public async Task Logout_IsIdempotent_CanCallMultipleTimes()
    {
        // Arrange
        var args = "logout localhost:5000";

        // Act
        var result1 = await new CliRunner().ExecuteAsync(args);
        var result2 = await new CliRunner().ExecuteAsync(args);

        // Assert
        result1.ExitCode.Should().Be(0, "first logout should succeed");
        result2.ExitCode.Should().Be(0, "second logout should also succeed");
    }
}
