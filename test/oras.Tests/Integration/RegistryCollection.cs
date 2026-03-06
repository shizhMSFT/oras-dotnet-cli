using Xunit;
using Oras.Tests.Fixtures;

namespace Oras.Tests.Integration;

/// <summary>
/// Collection definition for integration tests that share a single OCI registry container.
/// </summary>
/// <remarks>
/// Use this collection definition to share a registry fixture across multiple test classes.
/// Each test class participating in this collection will receive the same RegistryFixture instance,
/// reducing container startup overhead.
///
/// Example usage:
/// <code>
/// [Collection("Registry collection")]
/// public class RegistryPushTests
/// {
///     private readonly RegistryFixture _fixture;
///
///     public RegistryPushTests(RegistryFixture fixture)
///     {
///         _fixture = fixture;
///     }
/// }
///
/// [Collection("Registry collection")]
/// public class RegistryPullTests
/// {
///     private readonly RegistryFixture _fixture;
///
///     public RegistryPullTests(RegistryFixture fixture)
///     {
///         _fixture = fixture;
///     }
/// }
/// </code>
/// </remarks>
#pragma warning disable CA1515 // Collection definitions must be public for xUnit discovery
[CollectionDefinition("Registry collection")]
public class RegistryCollectionDefinition : ICollectionFixture<RegistryFixture>
{
    // This class exists only to define the collection and fixtures.
    // No test code goes here.
}
#pragma warning restore CA1515
