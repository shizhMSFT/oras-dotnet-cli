using Xunit;
using Oras.Tests.Fixtures;
using Oras.Tests.Helpers;

namespace Oras.Tests.Integration;

/// <summary>
/// Base class for integration tests that use a shared registry container.
/// </summary>
[Collection("Registry collection")]
public abstract class RegistryIntegrationTestBase : IAsyncLifetime
{
    protected readonly RegistryFixture Registry;
    protected readonly CliRunner Cli;

    protected RegistryIntegrationTestBase(RegistryFixture registry)
    {
        Registry = registry;
        Cli = new CliRunner();
    }

    /// <summary>
    /// Gets a unique repository name for this test to avoid conflicts.
    /// </summary>
    protected string GetTestRepository()
    {
        var testClass = GetType().Name;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"test-{testClass.ToLowerInvariant()}-{timestamp}";
    }

    /// <summary>
    /// Gets a full registry reference (host:port/repository:tag).
    /// </summary>
    protected string GetRegistryReference(string repository, string tag = "latest")
    {
        var host = Registry.RegistryUrl.Host;
        var port = Registry.RegistryUrl.Port;
        return $"{host}:{port}/{repository}:{tag}";
    }

    /// <summary>
    /// Creates a temporary test file with the specified content.
    /// </summary>
    protected async Task<string> CreateTestFileAsync(string content = "test content", string? fileName = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "oras-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, fileName ?? "test-file.txt");
        await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);

        return filePath;
    }

    /// <summary>
    /// Creates a temporary directory for test artifacts.
    /// </summary>
    protected string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "oras-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
