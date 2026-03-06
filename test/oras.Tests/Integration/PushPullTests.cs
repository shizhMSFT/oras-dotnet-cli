using FluentAssertions;
using Xunit;
using Oras.Tests.Fixtures;

namespace Oras.Tests.Integration;

/// <summary>
/// Integration tests for push and pull commands.
/// Tests the complete roundtrip workflow: push artifact, pull it back, verify contents.
/// </summary>
public sealed class PushPullTests : RegistryIntegrationTestBase
{
    public PushPullTests(RegistryFixture registry) : base(registry)
    {
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PushPull_SingleFile_RoundtripSucceeds()
    {
        // Arrange
        var repository = GetTestRepository();
        var reference = GetRegistryReference(repository, "v1");
        var originalContent = "Hello ORAS integration test!";
        var originalFile = await CreateTestFileAsync(originalContent, "hello.txt").ConfigureAwait(false);
        var pullDir = CreateTempDirectory();

        try
        {
            // Act - Push
            var pushResult = await Cli.ExecuteAsync($"push {reference} {originalFile}").ConfigureAwait(false);
            
            // Assert - Push succeeded
            pushResult.ExitCode.Should().Be(0, "push should succeed");

            // Act - Pull
            var pullResult = await Cli.ExecuteAsync($"pull {reference} -o {pullDir}").ConfigureAwait(false);

            // Assert - Pull succeeded
            pullResult.ExitCode.Should().Be(0, "pull should succeed");

            // Assert - File exists and content matches
            var pulledFile = Path.Combine(pullDir, "hello.txt");
            File.Exists(pulledFile).Should().BeTrue("pulled file should exist");
            var pulledContent = await File.ReadAllTextAsync(pulledFile).ConfigureAwait(false);
            pulledContent.Should().Be(originalContent, "pulled content should match pushed content");
        }
        finally
        {
            // Cleanup
            CleanupPath(Path.GetDirectoryName(originalFile));
            CleanupPath(pullDir);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Push_MultipleFiles_AllFilesArePushed()
    {
        // Arrange
        var repository = GetTestRepository();
        var reference = GetRegistryReference(repository, "multi");
        var tempDir = CreateTempDirectory();
        
        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.txt");
        await File.WriteAllTextAsync(file1, "Content 1").ConfigureAwait(false);
        await File.WriteAllTextAsync(file2, "Content 2").ConfigureAwait(false);

        try
        {
            // Act
            var pushResult = await Cli.ExecuteAsync($"push {reference} {file1} {file2}").ConfigureAwait(false);

            // Assert
            pushResult.ExitCode.Should().Be(0, "push with multiple files should succeed");
        }
        finally
        {
            CleanupPath(tempDir);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pull_NonexistentReference_Fails()
    {
        // Arrange
        var reference = GetRegistryReference("nonexistent-repo", "nonexistent-tag");
        var pullDir = CreateTempDirectory();

        try
        {
            // Act
            var pullResult = await Cli.ExecuteAsync($"pull {reference} -o {pullDir}").ConfigureAwait(false);

            // Assert
            pullResult.ExitCode.Should().NotBe(0, "pull of nonexistent reference should fail");
            pullResult.StandardError.Should().NotBeNullOrEmpty("error message should be provided");
        }
        finally
        {
            CleanupPath(pullDir);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Push_WithInvalidReference_Fails()
    {
        // Arrange
        var invalidReference = "invalid reference with spaces";
        var testFile = await CreateTestFileAsync().ConfigureAwait(false);

        try
        {
            // Act
            var pushResult = await Cli.ExecuteAsync($"push \"{invalidReference}\" {testFile}").ConfigureAwait(false);

            // Assert
            pushResult.ExitCode.Should().NotBe(0, "push with invalid reference should fail");
            // Exit code 2 = argument error per DEC-PRD-006
            pushResult.ExitCode.Should().Be(2, "invalid argument should return exit code 2");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Push_ToNonexistentRegistry_Fails()
    {
        // Arrange
        var reference = "nonexistent-registry.invalid:5000/test-repo:v1";
        var testFile = await CreateTestFileAsync().ConfigureAwait(false);

        try
        {
            // Act
            var pushResult = await Cli.ExecuteAsync($"push {reference} {testFile}", timeoutSeconds: 10).ConfigureAwait(false);

            // Assert
            pushResult.ExitCode.Should().NotBe(0, "push to nonexistent registry should fail");
            pushResult.StandardError.Should().NotBeNullOrEmpty("error message should be provided");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    private static void CleanupPath(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
