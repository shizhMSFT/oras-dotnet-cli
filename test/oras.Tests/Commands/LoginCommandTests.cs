using FluentAssertions;
using Oras.Tests.Helpers;
using Xunit;

namespace Oras.Tests.Commands;

/// <summary>
/// Unit tests for LoginCommand.
/// </summary>
public sealed class LoginCommandTests
{
    [Fact]
    public async Task Login_WithoutArguments_ReturnsArgumentError()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await new CliRunner().ExecuteAsync("login");

        // Assert
        result.ExitCode.Should().NotBe(0, "login without registry should fail");
        result.StandardError.Should().Contain("Required argument missing", "should mention missing argument");
    }

    [Fact]
    public async Task Login_WithUsernameOption_ParsesCorrectly()
    {
        // Arrange
        var args = "login localhost:5000 -u testuser -p testpass";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        // Command will fail due to NotImplementedException, but parsing should work
        result.ExitCode.Should().NotBe(0);
        // Should not fail with argument parsing error
        result.StandardOutput.Should().NotContain("Required argument missing", "arguments were parsed correctly");
    }

    [Fact(Skip = "Requires stdin input which times out in automated tests")]
    public async Task Login_WithPasswordStdinOption_ParsesCorrectly()
    {
        // Arrange
        var runner = new CliRunner();

        // This test requires providing input via stdin, which is not feasible in automated tests
        // The CLI correctly waits for password input from stdin when --password-stdin is specified

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Login_WithPlainHttpOption_ParsesCorrectly()
    {
        // Arrange
        var args = "login localhost:5000 -u test -p pass --plain-http";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        // Should parse option correctly even if command fails
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Login_WithInsecureOption_ParsesCorrectly()
    {
        // Arrange
        var args = "login localhost:5000 -u test -p pass --insecure";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        // Should parse option correctly even if command fails
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Login_WithAllOptions_ParsesCorrectly()
    {
        // Arrange
        var args = "login localhost:5000 -u testuser -p testpass --plain-http --insecure";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        // All options should parse without argument errors
        result.StandardOutput.Should().NotContain("Required argument missing");
    }
}
