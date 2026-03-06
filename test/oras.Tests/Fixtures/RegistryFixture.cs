using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Oras.Tests.Fixtures;

/// <summary>
/// Manages the lifecycle of a containerized OCI-compliant registry (Docker Distribution v3)
/// for integration testing.
/// </summary>
/// <remarks>
/// This fixture starts a registry container before tests run and cleans it up afterward.
/// Use with xUnit's IClassFixture{T} or ICollectionFixture{T} for shared registry setup.
///
/// Example usage with class fixture:
/// <code>
/// public class MyRegistryTests : IClassFixture{RegistryFixture}
/// {
///     private readonly RegistryFixture _fixture;
///
///     public MyRegistryTests(RegistryFixture fixture)
///     {
///         _fixture = fixture;
///     }
///
///     [Fact]
///     public async Task TestPushArtifact()
///     {
///         var registryUrl = _fixture.RegistryUrl;
///         // ... use registryUrl
///     }
/// }
/// </code>
/// </remarks>
internal sealed class RegistryFixture : IAsyncLifetime
{
    private const string DefaultImage = "ghcr.io/distribution/distribution:3.0.0";
    private const int RegistryPort = 5000;

    private IContainer? _container;

    /// <summary>
    /// Gets the base URL of the running registry (e.g., http://localhost:random_port).
    /// Available after InitializeAsync() completes.
    /// </summary>
    public Uri RegistryUrl { get; private set; } = null!;

    /// <summary>
    /// Creates a new RegistryFixture with default configuration.
    /// </summary>
    public RegistryFixture()
        : this(DefaultImage)
    {
    }

    /// <summary>
    /// Creates a new RegistryFixture with a custom registry image.
    /// </summary>
    /// <param name="image">Docker image name (e.g., "ghcr.io/distribution/distribution:3.0.0")</param>
    public RegistryFixture(string image)
    {
        Image = image;
    }

    private string Image { get; }

    /// <summary>
    /// Starts the registry container and sets RegistryUrl.
    /// Called automatically by xUnit before tests run.
    /// </summary>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage(Image)
            .WithPortBinding(RegistryPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                .ForPort(RegistryPort)
                .ForPath("/v2/")))
            .Build();

        await _container.StartAsync().ConfigureAwait(false);

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(RegistryPort);
        RegistryUrl = new Uri($"http://{host}:{port}");
    }

    /// <summary>
    /// Stops and removes the registry container.
    /// Called automatically by xUnit after tests finish.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Container already disposed, suppress
        }
    }

    /// <summary>
    /// Lists all repositories in the registry.
    /// </summary>
    /// <returns>Array of repository names, or empty array if no repositories exist.</returns>
    /// <exception cref="InvalidOperationException">Thrown if registry is not running.</exception>
    /// <exception cref="HttpRequestException">Thrown if the request fails.</exception>
    public async Task<string[]> ListRepositoriesAsync()
    {
        EnsureRunning();

        using var client = new HttpClient();
        var uri = new Uri(RegistryUrl, "/v2/_catalog");
        var response = await client.GetAsync(uri).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var catalogResponse = await response.Content.ReadFromJsonAsync<CatalogResponse>().ConfigureAwait(false);
        return catalogResponse?.Repositories ?? Array.Empty<string>();
    }

    /// <summary>
    /// Lists all tags for a given repository.
    /// </summary>
    /// <param name="repository">Repository name (e.g., "my-image")</param>
    /// <returns>Array of tag names, or empty array if no tags exist.</returns>
    /// <exception cref="InvalidOperationException">Thrown if registry is not running.</exception>
    /// <exception cref="ArgumentException">Thrown if repository name is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown if the request fails.</exception>
    public async Task<string[]> ListTagsAsync(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new ArgumentException("Repository name cannot be null or empty.", nameof(repository));
        }

        EnsureRunning();

        using var client = new HttpClient();
        var uri = new Uri(RegistryUrl, $"/v2/{repository}/tags/list");
        var response = await client.GetAsync(uri).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var tagResponse = await response.Content.ReadFromJsonAsync<TagListResponse>().ConfigureAwait(false);
        return tagResponse?.Tags ?? Array.Empty<string>();
    }

    /// <summary>
    /// Checks if the registry is healthy by calling the health check endpoint.
    /// </summary>
    /// <returns>True if registry is responding to /v2/ requests; false otherwise.</returns>
    public async Task<bool> IsHealthyAsync()
    {
        if (RegistryUrl is null)
        {
            return false;
        }

        try
        {
            using var client = new HttpClient();
            var uri = new Uri(RegistryUrl, "/v2/");
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private void EnsureRunning()
    {
        if (RegistryUrl is null)
        {
            throw new InvalidOperationException(
                "Registry is not running. Ensure InitializeAsync() has been called by xUnit."
            );
        }
    }

    /// <summary>
    /// Internal response types for registry API calls.
    /// </summary>
    private sealed class CatalogResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("repositories")]
        public string[]? Repositories { get; set; }
    }

    private sealed class TagListResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }
}
