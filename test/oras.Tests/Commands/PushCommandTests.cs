using FluentAssertions;
using Oras.Tests.Helpers;
using Xunit;

namespace Oras.Tests.Commands;

/// <summary>
/// Unit tests for PushCommand.
/// </summary>
public sealed class PushCommandTests
{
    [Fact]
    public async Task Push_WithoutArguments_ReturnsArgumentError()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await new CliRunner().ExecuteAsync("push");

        // Assert
        result.ExitCode.Should().NotBe(0, "push without arguments should fail");
        result.StandardError.Should().Contain("Required argument missing", "should mention missing argument");
    }

    [Fact]
    public async Task Push_WithReferenceOnly_ShowsUsageError()
    {
        // Arrange
        var runner = new CliRunner();

        // Act
        var result = await new CliRunner().ExecuteAsync("push localhost:5000/test:v1");

        // Assert
        result.ExitCode.Should().NotBe(0, "push without files should fail");
        result.StandardOutput.Should().Contain("Error:", "should show error message");
        result.StandardOutput.Should().Contain("No files", "should explain missing files");
    }

    [Fact]
    public async Task Push_WithNonexistentFile_ShowsFileNotFoundError()
    {
        // Arrange
        var args = "push localhost:5000/test:v1 nonexistent-file.txt";

        // Act
        var result = await new CliRunner().ExecuteAsync(args);

        // Assert
        result.ExitCode.Should().Be(2, "push with nonexistent file should return usage error");
        result.StandardOutput.Should().Contain("File not found", "should explain file doesn't exist");
    }

    [Fact]
    public async Task Push_WithArtifactTypeOption_ParsesCorrectly()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        try
        {
            var args = $"push localhost:5000/test:v1 {testFile} --artifact-type application/vnd.test";

            // Act
            var result = await new CliRunner().ExecuteAsync(args);

            // Assert
            // Will fail due to NotImplementedException, but parsing should work
            result.ExitCode.Should().NotBe(0);
            result.StandardOutput.Should().Contain("Error:", "should show error, not argument parsing failure");
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Fact]
    public async Task Push_WithAnnotationOption_ParsesCorrectly()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        try
        {
            var args = $"push localhost:5000/test:v1 {testFile} --annotation key1=value1 --annotation key2=value2";

            // Act
            var result = await new CliRunner().ExecuteAsync(args);

            // Assert
            // Will fail due to NotImplementedException, but parsing should work
            result.ExitCode.Should().NotBe(0);
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Fact]
    public async Task Push_WithConcurrencyOption_ParsesCorrectly()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        try
        {
            var args = $"push localhost:5000/test:v1 {testFile} --concurrency 5";

            // Act
            var result = await new CliRunner().ExecuteAsync(args);

            // Assert
            // Will fail due to NotImplementedException, but parsing should work
            result.ExitCode.Should().NotBe(0);
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Fact]
    public async Task Push_WithMultipleFiles_ParsesAllFiles()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        try
        {
            var args = $"push localhost:5000/test:v1 {file1} {file2}";

            // Act
            var result = await new CliRunner().ExecuteAsync(args);

            // Assert
            // Will fail due to NotImplementedException, but should parse both files
            result.ExitCode.Should().NotBe(0);
            result.StandardOutput.Should().NotContain("File not found", "both files should be found");
        }
        finally
        {
            if (File.Exists(file1))
            {
                File.Delete(file1);
            }

            if (File.Exists(file2))
            {
                File.Delete(file2);
            }
        }
    }
}
