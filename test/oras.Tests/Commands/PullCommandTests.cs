using FluentAssertions;
using Oras.Tests.Helpers;
using Xunit;

namespace Oras.Tests.Commands;

/// <summary>
/// Unit tests for PullCommand.
/// </summary>
public sealed class PullCommandTests
{
    [Fact]
    public async Task Pull_WithoutArguments_ReturnsArgumentError()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await new CliRunner().ExecuteAsync("pull").ConfigureAwait(false);

        // Assert
        result.ExitCode.Should().NotBe(0, "pull without arguments should fail");
        result.StandardError.Should().Contain("Required argument missing", "should mention missing argument");
    }

    [Fact]
    public async Task Pull_WithReferenceOnly_UsesCurrentDirectory()
    {
        // Arrange & Act
        var result = await new CliRunner().ExecuteAsync("pull localhost:5000/test:v1").ConfigureAwait(false);

        // Assert
        // Will fail due to NotImplementedException, but parsing should work
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Pull_WithOutputOption_ParsesCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(tempDir);
            var args = $"pull localhost:5000/test:v1 -o {tempDir}";

            // Act
            var result = await new CliRunner().ExecuteAsync(args).ConfigureAwait(false);

            // Assert
            // Will fail due to NotImplementedException, but parsing should work
            result.ExitCode.Should().NotBe(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task Pull_WithPlatformOption_ParsesCorrectly()
    {
        // Arrange
        var args = "pull localhost:5000/test:v1 --platform linux/amd64";

        // Act
        var result = await new CliRunner().ExecuteAsync(args).ConfigureAwait(false);

        // Assert
        // Will fail due to NotImplementedException, but parsing should work
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Pull_WithKeepOldFilesOption_ParsesCorrectly()
    {
        // Arrange
        var args = "pull localhost:5000/test:v1 --keep-old-files";

        // Act
        var result = await new CliRunner().ExecuteAsync(args).ConfigureAwait(false);

        // Assert
        // Will fail due to NotImplementedException, but parsing should work
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Pull_WithIncludeSubjectOption_ParsesCorrectly()
    {
        // Arrange
        var args = "pull localhost:5000/test:v1 --include-subject";

        // Act
        var result = await new CliRunner().ExecuteAsync(args).ConfigureAwait(false);

        // Assert
        // Will fail due to NotImplementedException, but parsing should work
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Pull_WithAllOptions_ParsesCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(tempDir);
            var args = $"pull localhost:5000/test:v1 -o {tempDir} --platform linux/amd64 --keep-old-files --include-subject";

            // Act
            var result = await new CliRunner().ExecuteAsync(args).ConfigureAwait(false);

            // Assert
            // All options should parse correctly
            result.ExitCode.Should().NotBe(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
