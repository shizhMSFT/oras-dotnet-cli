using FluentAssertions;
using Xunit;
using Oras.Tests.Fixtures;

namespace Oras.Tests.Integration;

/// <summary>
/// Smoke tests for basic CLI functionality.
/// </summary>
public sealed class VersionCommandTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Version_WithNoArgs_ReturnsVersionInfo()
    {
        // Arrange
        var cli = new Helpers.CliRunner();

        // Act
        var result = await cli.ExecuteAsync("version").ConfigureAwait(false);

        // Assert
        result.ExitCode.Should().Be(0, "version command should succeed");
        result.StandardOutput.Should().NotBeNullOrEmpty("version should output version information");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Version_WithHelpFlag_ShowsHelp()
    {
        // Arrange
        var cli = new Helpers.CliRunner();

        // Act
        var result = await cli.ExecuteAsync("version --help").ConfigureAwait(false);

        // Assert
        result.ExitCode.Should().Be(0, "help should succeed");
        result.StandardOutput.Should().Contain("version", "help should mention the version command");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Help_WithNoArgs_ShowsGeneralHelp()
    {
        // Arrange
        var cli = new Helpers.CliRunner();

        // Act
        var result = await cli.ExecuteAsync("--help").ConfigureAwait(false);

        // Assert
        result.ExitCode.Should().Be(0, "help should succeed");
        result.StandardOutput.Should().Contain("oras", "help should mention oras");
    }
}
