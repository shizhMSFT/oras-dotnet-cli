using FluentAssertions;
using Xunit;
using Oras.Tests.Fixtures;

namespace Oras.Tests.Integration;

/// <summary>
/// Integration tests for Sprint 2 commands.
/// Note: Most implementations currently throw NotImplementedException.
/// Tests are written to validate expected behavior once implementations are complete.
/// </summary>
public sealed class Sprint2CommandTests : RegistryIntegrationTestBase
{
    public Sprint2CommandTests(RegistryFixture registry) : base(registry)
    {
    }

    #region Tag Command Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TagCommand_MultipleTargetTags_AllTagsCreated()
    {
        // Arrange - requires a pushed artifact first
        var repository = GetTestRepository();
        var sourceRef = GetRegistryReference(repository, "v1");
        var testFile = await CreateTestFileAsync("tag test content").ConfigureAwait(false);

        try
        {
            // First push an artifact (will fail with NotImplementedException, but test is ready)
            await Cli.ExecuteAsync($"push {sourceRef} {testFile}").ConfigureAwait(false);

            // Act - Tag with multiple tags
            var tagResult = await Cli.ExecuteAsync($"tag {sourceRef} v2 v3 latest").ConfigureAwait(false);

            // Assert - Currently fails due to NotImplementedException
            // TODO: Once implemented, should return exit code 0 and verify all tags exist
            tagResult.ExitCode.Should().Be(1, "tag currently returns 1 due to NotImplementedException");
            tagResult.StandardOutput.Should().Contain("Error:", "error should be shown");
            tagResult.StandardOutput.Should().Contain("not yet implemented", "should indicate implementation pending");

            // TODO: Once implemented, verify tags exist:
            // var tags = await Registry.ListTagsAsync(repository);
            // tags.Should().Contain(new[] { "v1", "v2", "v3", "latest" });
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TagCommand_WithDigest_TagsManifestByDigest()
    {
        // Arrange
        var repository = GetTestRepository();
        var sourceDigest = "@sha256:1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var sourceRef = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}/{repository}{sourceDigest}";

        // Act
        var tagResult = await Cli.ExecuteAsync($"tag {sourceRef} newtag").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        tagResult.ExitCode.Should().Be(1, "tag currently returns 1 due to NotImplementedException");
        tagResult.StandardOutput.Should().Contain("Error:");
    }

    #endregion

    #region Copy Command Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CopyCommand_SameRegistry_CopiesArtifactBetweenRepositories()
    {
        // Arrange
        var sourceRepo = GetTestRepository();
        var destRepo = $"{sourceRepo}-copy";
        var sourceRef = GetRegistryReference(sourceRepo, "v1");
        var destRef = GetRegistryReference(destRepo, "v1");
        var testFile = await CreateTestFileAsync("copy test content").ConfigureAwait(false);

        try
        {
            // Push source artifact first (will fail, but test structure is ready)
            await Cli.ExecuteAsync($"push {sourceRef} {testFile}").ConfigureAwait(false);

            // Act - Copy between repositories
            var copyResult = await Cli.ExecuteAsync($"copy {sourceRef} {destRef}").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should return exit code 0 and artifact should exist in dest
            copyResult.ExitCode.Should().Be(1, "copy currently returns 1 due to NotImplementedException");
            copyResult.StandardOutput.Should().Contain("Error:");
            copyResult.StandardOutput.Should().Contain("not yet implemented");

            // TODO: Once implemented, verify destination:
            // var tags = await Registry.ListTagsAsync(destRepo);
            // tags.Should().Contain("v1");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CopyCommand_WithRecursiveFlag_CopiesAllReferrers()
    {
        // Arrange
        var sourceRef = GetRegistryReference(GetTestRepository(), "v1");
        var destRef = GetRegistryReference(GetTestRepository(), "v2");

        // Act - Copy with recursive flag
        var copyResult = await Cli.ExecuteAsync($"copy --recursive {sourceRef} {destRef}").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        copyResult.ExitCode.Should().Be(1);
        copyResult.StandardOutput.Should().Contain("Error:");
        
        // TODO: Once implemented, verify all referrers were copied
    }

    #endregion

    #region Repo Commands Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RepoLs_AfterPushingArtifacts_ListsRepositories()
    {
        // Arrange
        var host = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";
        var repo1 = GetTestRepository();
        var repo2 = GetTestRepository();
        var testFile = await CreateTestFileAsync("repo ls test").ConfigureAwait(false);

        try
        {
            // Push artifacts to multiple repositories (will fail, but test is ready)
            await Cli.ExecuteAsync($"push {GetRegistryReference(repo1, "v1")} {testFile}").ConfigureAwait(false);
            await Cli.ExecuteAsync($"push {GetRegistryReference(repo2, "v1")} {testFile}").ConfigureAwait(false);

            // Act - List repositories
            var lsResult = await Cli.ExecuteAsync($"repo ls {host} --plain-http").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should list all repositories
            lsResult.ExitCode.Should().Be(1, "repo ls currently returns 1 due to NotImplementedException");
            lsResult.StandardOutput.Should().Contain("Error:");

            // TODO: Once implemented, verify output contains repositories:
            // lsResult.StandardOutput.Should().Contain(repo1);
            // lsResult.StandardOutput.Should().Contain(repo2);
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RepoTags_AfterPushingMultipleTags_ListsAllTags()
    {
        // Arrange
        var repository = GetTestRepository();
        var ref1 = GetRegistryReference(repository, "v1");
        var ref2 = GetRegistryReference(repository, "v2");
        var testFile = await CreateTestFileAsync("tags test").ConfigureAwait(false);

        try
        {
            // Push with multiple tags (will fail, but test structure is ready)
            await Cli.ExecuteAsync($"push {ref1} {testFile}").ConfigureAwait(false);
            await Cli.ExecuteAsync($"push {ref2} {testFile}").ConfigureAwait(false);

            // Act - List tags
            var repoRef = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}/{repository}";
            var tagsResult = await Cli.ExecuteAsync($"repo tags {repoRef} --plain-http").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should list all tags
            tagsResult.ExitCode.Should().Be(1, "repo tags currently returns 1 due to NotImplementedException");
            tagsResult.StandardOutput.Should().Contain("Error:");

            // TODO: Once implemented, verify tags are listed:
            // tagsResult.StandardOutput.Should().Contain("v1");
            // tagsResult.StandardOutput.Should().Contain("v2");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    #endregion

    #region Manifest Operations Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManifestFetch_ExistingManifest_ReturnsManifestJson()
    {
        // Arrange
        var repository = GetTestRepository();
        var reference = GetRegistryReference(repository, "v1");
        var testFile = await CreateTestFileAsync("manifest test").ConfigureAwait(false);

        try
        {
            // Push artifact first (will fail, but test structure is ready)
            await Cli.ExecuteAsync($"push {reference} {testFile}").ConfigureAwait(false);

            // Act - Fetch manifest
            var fetchResult = await Cli.ExecuteAsync($"manifest fetch {reference} --plain-http").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should return manifest JSON
            fetchResult.ExitCode.Should().Be(1, "manifest fetch currently returns 1 due to NotImplementedException");
            fetchResult.StandardOutput.Should().Contain("Error:");

            // TODO: Once implemented, verify manifest content:
            // fetchResult.StandardOutput.Should().Contain("schemaVersion");
            // fetchResult.StandardOutput.Should().Contain("mediaType");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManifestFetch_WithDescriptorFlag_ReturnsDescriptorOnly()
    {
        // Arrange
        var reference = GetRegistryReference(GetTestRepository(), "v1");

        // Act - Fetch with --descriptor flag
        var fetchResult = await Cli.ExecuteAsync($"manifest fetch {reference} --descriptor --plain-http").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        fetchResult.ExitCode.Should().Be(1);
        fetchResult.StandardOutput.Should().Contain("Error:");

        // TODO: Once implemented, should return descriptor JSON with digest, mediaType, size
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManifestFetch_WithPrettyFlag_ReturnsPrettyPrintedJson()
    {
        // Arrange
        var reference = GetRegistryReference(GetTestRepository(), "v1");

        // Act - Fetch with --pretty flag
        var fetchResult = await Cli.ExecuteAsync($"manifest fetch {reference} --pretty --plain-http").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        fetchResult.ExitCode.Should().Be(1);
        fetchResult.StandardOutput.Should().Contain("Error:");

        // TODO: Once implemented, verify JSON is indented with whitespace
    }

    #endregion

    #region Blob Operations Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlobPush_ValidFile_UploadsBlobAndReturnsDigest()
    {
        // Arrange
        var repository = GetTestRepository();
        var reference = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}/{repository}";
        var testFile = await CreateTestFileAsync("blob push test content").ConfigureAwait(false);

        try
        {
            // Act - Push blob
            var pushResult = await Cli.ExecuteAsync($"blob push {reference} {testFile} --plain-http").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should return exit code 0 and display digest
            pushResult.ExitCode.Should().Be(1, "blob push currently returns 1 due to NotImplementedException");
            pushResult.StandardOutput.Should().Contain("Error:");

            // TODO: Once implemented, verify digest in output:
            // pushResult.StandardOutput.Should().Contain("sha256:");
            // pushResult.StandardOutput.Should().Contain("Uploaded");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlobFetch_ExistingBlob_DownloadsBlobContent()
    {
        // Arrange - requires pushing a blob first, then getting its digest
        var repository = GetTestRepository();
        var digest = "sha256:1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var reference = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}/{repository}@{digest}";
        var outputFile = Path.Combine(CreateTempDirectory(), "downloaded-blob");

        try
        {
            // Act - Fetch blob
            var fetchResult = await Cli.ExecuteAsync($"blob fetch {reference} --output {outputFile} --plain-http").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should download blob to file
            fetchResult.ExitCode.Should().Be(1, "blob fetch currently returns 1 due to NotImplementedException");
            fetchResult.StandardOutput.Should().Contain("Error:");

            // TODO: Once implemented, verify file exists and content matches
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(outputFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlobDelete_WithForceFlag_DeletesBlob()
    {
        // Arrange
        var repository = GetTestRepository();
        var digest = "sha256:1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var reference = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}/{repository}@{digest}";

        // Act - Delete with --force flag
        var deleteResult = await Cli.ExecuteAsync($"blob delete {reference} --force --plain-http").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        // TODO: Once implemented, should delete blob
        deleteResult.ExitCode.Should().Be(1, "blob delete currently returns 1 due to NotImplementedException");
        deleteResult.StandardOutput.Should().Contain("Error:");

        // TODO: Once implemented, verify blob no longer exists
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlobDelete_WithoutForceFlag_FailsInNonInteractiveMode()
    {
        // Arrange
        var repository = GetTestRepository();
        var digest = "sha256:abcdef";
        var reference = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}/{repository}@{digest}";

        // Act - Delete without --force in non-interactive mode
        var deleteResult = await Cli.ExecuteAsync($"blob delete {reference} --plain-http").ConfigureAwait(false);

        // Assert - Should fail (currently returns 1 due to NotImplementedException being thrown before --force validation)
        deleteResult.ExitCode.Should().NotBe(0, "blob delete should fail without --force flag");
        deleteResult.StandardOutput.Should().Contain("Error:");
        
        // TODO: Once NotImplementedException is removed, should return exit code 2 (UsageException)
        // and show message: "Deletion requires --force flag in non-interactive mode"
    }

    #endregion

    #region Discover Command Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DiscoverCommand_ArtifactWithReferrers_ListsAllReferrers()
    {
        // Arrange - requires pushing an artifact with referrers (SBOM, signature, etc.)
        var repository = GetTestRepository();
        var reference = GetRegistryReference(repository, "v1");
        var testFile = await CreateTestFileAsync("discover test").ConfigureAwait(false);

        try
        {
            // Push base artifact (will fail, but test structure is ready)
            await Cli.ExecuteAsync($"push {reference} {testFile}").ConfigureAwait(false);

            // Push referrers (signature, SBOM) - would use attach command
            // await Cli.ExecuteAsync($"attach {reference} sbom.json --artifact-type application/vnd.example.sbom");

            // Act - Discover referrers
            var discoverResult = await Cli.ExecuteAsync($"discover {reference} --plain-http").ConfigureAwait(false);

            // Assert - Currently fails with NotImplementedException
            // TODO: Once implemented, should list all referrers
            discoverResult.ExitCode.Should().Be(1, "discover currently returns 1 due to NotImplementedException");
            discoverResult.StandardOutput.Should().Contain("Error:");

            // TODO: Once implemented, verify referrers in output:
            // discoverResult.StandardOutput.Should().Contain("application/vnd.example.sbom");
            // discoverResult.StandardOutput.Should().Contain("sha256:");
        }
        finally
        {
            CleanupPath(Path.GetDirectoryName(testFile));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DiscoverCommand_WithArtifactTypeFilter_FiltersReferrers()
    {
        // Arrange
        var reference = GetRegistryReference(GetTestRepository(), "v1");

        // Act - Discover with artifact type filter
        var discoverResult = await Cli.ExecuteAsync(
            $"discover {reference} --artifact-type application/vnd.example.sbom --plain-http").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        discoverResult.ExitCode.Should().Be(1);
        discoverResult.StandardOutput.Should().Contain("Error:");

        // TODO: Once implemented, should only show SBOM referrers
    }

    #endregion

    #region JSON Output Format Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CopyCommand_WithJsonFormat_OutputsJsonDescriptor()
    {
        // Arrange
        var sourceRef = GetRegistryReference(GetTestRepository(), "v1");
        var destRef = GetRegistryReference(GetTestRepository(), "v2");

        // Act - Copy with --format json
        var copyResult = await Cli.ExecuteAsync($"copy {sourceRef} {destRef} --format json").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        copyResult.ExitCode.Should().Be(1);
        copyResult.StandardOutput.Should().Contain("Error:");

        // TODO: Once implemented, verify JSON output structure
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RepoLs_WithJsonFormat_OutputsJsonArray()
    {
        // Arrange
        var host = $"{Registry.RegistryUrl.Host}:{Registry.RegistryUrl.Port}";

        // Act - List with --format json
        var lsResult = await Cli.ExecuteAsync($"repo ls {host} --format json --plain-http").ConfigureAwait(false);

        // Assert - Currently fails with NotImplementedException
        lsResult.ExitCode.Should().Be(1);
        lsResult.StandardOutput.Should().Contain("Error:");

        // TODO: Once implemented, should return JSON array of repository names
    }

    #endregion

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
