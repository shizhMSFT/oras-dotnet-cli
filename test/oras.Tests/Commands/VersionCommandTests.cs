using FluentAssertions;
using Oras.Tests.Helpers;
using Xunit;

namespace Oras.Tests.Commands;

/// <summary>
/// Unit tests for VersionCommand.
/// </summary>
public sealed class VersionCommandTests
{
    [Fact]
    public async Task Version_WithNoArgs_ReturnsSuccessExitCode()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await runner.ExecuteAsync("version");

        // Assert
        result.ExitCode.Should().Be(0, "version command should succeed");
    }

    [Fact]
    public async Task Version_WithNoArgs_DisplaysVersionTable()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await runner.ExecuteAsync("version");

        // Assert
        result.StandardOutput.Should().Contain("CLI Version", "should show CLI version");
        result.StandardOutput.Should().Contain("Library Version", "should show library version");
        result.StandardOutput.Should().Contain(".NET Runtime", "should show runtime version");
        result.StandardOutput.Should().Contain("Platform", "should show platform info");
    }

    [Fact]
    public async Task Version_WithNoArgs_ShowsNonEmptyVersionNumber()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await runner.ExecuteAsync("version");

        // Assert
        result.StandardOutput.Should().NotBeNullOrWhiteSpace("version output should not be empty");
        // Version should contain digits
        result.StandardOutput.Should().MatchRegex(@"\d+\.\d+", "should contain version number pattern");
    }
}
