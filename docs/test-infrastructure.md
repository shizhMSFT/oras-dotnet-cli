# Test Infrastructure Design

**Status:** Reference Design  
**Last Updated:** 2025-03-06  
**Owner:** Hicks (Tester)

## Overview

This document outlines the recommended test infrastructure for ORAS .NET CLI, focusing on integration testing with containerized OCI registries using testcontainers-dotnet and xUnit.

---

## 1. Test Pyramid & Categories

```
     /\
    /  \ -- E2E Tests (CLI black-box, real registry, GitHub Actions only)
   /____\
  /      \
 /________\ -- Integration Tests (xUnit + testcontainers, class/collection fixtures)
/          \
/___________\ -- Unit Tests (Fast, isolated, no containers)
```

### Unit Tests
- **Scope:** Command argument parsing, model validation, business logic (no I/O)
- **Framework:** xUnit with standard `[Fact]` tests
- **Fixture:** Constructor/Dispose or `IAsyncLifetime` for non-container setup
- **Execution:** Local dev, CI, instant feedback

### Integration Tests
- **Scope:** ORAS CLI against real containerized OCI registry
- **Framework:** xUnit with testcontainers-dotnet for registry lifecycle
- **Fixture:** Class or collection fixtures for container management
- **Execution:** Local dev (with Docker), CI/CD pipelines
- **Example scenarios:**
  - Push artifacts to registry
  - Pull artifacts from registry
  - List repositories/tags
  - Authentication workflows
  - Tag operations
  - Copy operations

### E2E Tests (Optional, GitHub Actions only)
- **Scope:** Full CLI workflows against production-like registries
- **Framework:** Shell scripts or xUnit test helper classes
- **Execution:** Post-merge on main branch, scheduled runs
- **Target:** GitHub Container Registry (GHCR), Docker Hub, or public test registries

---

## 2. Testcontainers-dotnet Setup

### Package Version
```xml
<PackageReference Include="Testcontainers" Version="3.10.0" />
```

### OCI Registry Container Selection

**Recommended:** Docker Distribution Registry (`registry:2`)
```
Image: registry:2.8.3 (or latest patch)
Port: 5000
Protocol: HTTP (no TLS for tests, TLS optional for production)
Auth: Optional (htpasswd, token-based)
```

Why `registry:2`:
- OCI Distribution Spec compliant
- Lightweight, fast startup (~1-2s)
- No auth required by default (tests can add if needed)
- Wide ecosystem support
- Well-documented configuration

Alternative: Zot Registry (native OCI, advanced features)
```
Image: ghcr.io/project-zot/zot-linux-amd64:latest
Port: 5000
Features: Built-in auth, SBOMs, image signing
Note: More feature-rich but heavier than distribution/distribution
```

### Container Configuration Pattern

```csharp
using Testcontainers.Registry;

var registryContainer = new RegistryBuilder()
    .WithImage("registry:2.8.3")
    .WithPortBinding(5000, true)  // Random port binding
    .Build();

await registryContainer.StartAsync();

// Access registry
string registryHost = registryContainer.Hostname;
int port = registryContainer.GetMappedPublicPort(5000);
string registryUrl = $"http://{registryHost}:{port}";
```

### Wait Strategies

```csharp
// Wait for registry HTTP health check
.WithWaitStrategy(
    Wait.ForUnixContainer()
        .UntilHttpRequestIsSucceeded(r => r.ForPath("/v2/").ForPort(5000))
)
```

---

## 3. xUnit Fixture Patterns

### Pattern A: Class Fixture (Single test class)
**Use when:** One test class, one registry instance, expensive setup

```csharp
using Xunit;
using Testcontainers.Registry;

namespace Oras.Tests.Integration;

public class RegistryFixture : IAsyncLifetime
{
    private readonly IContainer _registry;
    
    public string RegistryUrl { get; private set; }

    public RegistryFixture()
    {
        _registry = new RegistryBuilder()
            .WithImage("registry:2.8.3")
            .WithPortBinding(5000, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPath("/v2/").ForPort(5000))
            )
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _registry.StartAsync();
        var port = _registry.GetMappedPublicPort(5000);
        RegistryUrl = $"http://{_registry.Hostname}:{port}";
    }

    public async Task DisposeAsync()
    {
        await _registry.StopAsync();
        await _registry.DisposeAsync();
    }
}

public class RegistryPushTests : IClassFixture<RegistryFixture>
{
    private readonly RegistryFixture _fixture;

    public RegistryPushTests(RegistryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PushArtifactToRegistry_Success()
    {
        // Arrange
        var registryUrl = _fixture.RegistryUrl;
        
        // Act & Assert
        // ... use registryUrl to test push operation
    }
}
```

### Pattern B: Collection Fixture (Multiple test classes, shared registry)
**Use when:** Multiple test classes, shared expensive registry setup

```csharp
using Xunit;
using Testcontainers.Registry;

namespace Oras.Tests.Integration;

public class RegistryFixture : IAsyncLifetime
{
    private readonly IContainer _registry;
    
    public string RegistryUrl { get; private set; }

    public RegistryFixture()
    {
        _registry = new RegistryBuilder()
            .WithImage("registry:2.8.3")
            .WithPortBinding(5000, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPath("/v2/").ForPort(5000))
            )
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _registry.StartAsync();
        var port = _registry.GetMappedPublicPort(5000);
        RegistryUrl = $"http://{_registry.Hostname}:{port}";
    }

    public async Task DisposeAsync()
    {
        await _registry.StopAsync();
        await _registry.DisposeAsync();
    }
}

[CollectionDefinition("Registry collection")]
public class RegistryCollection : ICollectionFixture<RegistryFixture>
{
    // Collection definition only — no code here
}

[Collection("Registry collection")]
public class RegistryPushTests
{
    private readonly RegistryFixture _fixture;

    public RegistryPushTests(RegistryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PushArtifactToRegistry_Success()
    {
        // Use _fixture.RegistryUrl
    }
}

[Collection("Registry collection")]
public class RegistryPullTests
{
    private readonly RegistryFixture _fixture;

    public RegistryPullTests(RegistryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PullArtifactFromRegistry_Success()
    {
        // Use _fixture.RegistryUrl
    }
}
```

### Fixture Lifecycle Comparison

| Scope | Lifetime | Use Case | Syntax |
|-------|----------|----------|--------|
| **Per-Test** | New instance per test | Clean state, fast setup | Constructor/Dispose |
| **Class** | One instance per test class | Expensive setup (registry), class-level sharing | `IClassFixture<T>` + `IAsyncLifetime` |
| **Collection** | One instance across test classes | Very expensive setup, multiple classes | `[CollectionDefinition]` + `[Collection]` |
| **Assembly** | One instance for entire assembly (xUnit v3) | Shared resource (rarely needed for containers) | `[assembly: AssemblyFixture(...)]` |

---

## 4. Recommended Test Directory Structure

```
test/
├── oras.Tests/
│   ├── oras.Tests.csproj
│   ├── Unit/
│   │   ├── CommandParsing/
│   │   │   └── PushCommandTests.cs
│   │   └── Models/
│   │       └── ArtifactTests.cs
│   ├── Integration/
│   │   ├── Fixtures/
│   │   │   ├── RegistryFixture.cs
│   │   │   ├── RegistryCollection.cs
│   │   │   └── AuthRegistryFixture.cs
│   │   ├── Push/
│   │   │   └── PushToRegistryTests.cs
│   │   ├── Pull/
│   │   │   └── PullFromRegistryTests.cs
│   │   ├── Tag/
│   │   │   └── TagOperationsTests.cs
│   │   └── Copy/
│   │       └── CopyOperationsTests.cs
│   ├── E2E/ (optional)
│   │   └── EndToEndTests.cs
│   └── SmokeTests.cs
```

---

## 5. CI/CD Configuration (GitHub Actions)

### Default: No Special Docker Setup Required

GitHub Actions Ubuntu runners have Docker pre-installed. testcontainers-dotnet will auto-detect and use it.

```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      
      - name: Build
        run: dotnet build
      
      - name: Run Unit Tests
        run: dotnet test --filter "Category!=Integration" --no-build
      
      - name: Run Integration Tests
        run: dotnet test --filter "Category=Integration" --no-build
```

### Filtering Tests

Use trait attributes to categorize tests:

```csharp
[Trait("Category", "Integration")]
public class RegistryPushTests
{
    // ...
}
```

### Optional: Testcontainers Cloud (Faster CI)

For faster CI runs, use Testcontainers Cloud to offload container workload:

```yaml
      - name: Setup Testcontainers Cloud
        uses: atomicjar/testcontainers-cloud-setup-action@v1
        with:
          token: ${{ secrets.TC_CLOUD_TOKEN }}
      
      - name: Run Integration Tests
        run: dotnet test --filter "Category=Integration" --no-build
```

### Logs & Diagnostics

Enable verbose output if containers fail:

```yaml
      - name: Run Integration Tests (with diagnostics)
        run: dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed" --no-build
```

---

## 6. Testing Against Real Registry (Optional)

For E2E or staging validation, you can test against external registries:

### Docker Hub
```csharp
var registryUrl = "https://registry-1.docker.io";
// Requires credentials stored in secrets
```

### GitHub Container Registry (GHCR)
```csharp
var registryUrl = "ghcr.io";
// Use GitHub Actions `${{ secrets.GITHUB_TOKEN }}`
```

### Local Registry (Manual)
```bash
docker run -d -p 5000:5000 --name test-registry registry:2.8.3
```

---

## 7. Best Practices

### Container Cleanup
✅ Always use `IAsyncLifetime` for proper cleanup
✅ Test containers are automatically removed after tests
⚠️ If tests crash, Docker may leave orphaned containers — use `docker ps -a` and `docker container prune`

### Port Binding
✅ Use random port binding (`WithPortBinding(5000, true)`) to avoid conflicts
✅ Get mapped port with `container.GetMappedPublicPort(5000)`
⚠️ Never hardcode ports (fails on CI if multiple tests run in parallel)

### Wait Strategies
✅ Use appropriate wait strategies for your registry:
```csharp
Wait.ForUnixContainer()
    .UntilHttpRequestIsSucceeded(r => r.ForPath("/v2/").ForPort(5000))
```
⚠️ Don't rely on fixed delays (`Thread.Sleep`) — use health checks instead

### Test Isolation
✅ Each test should be independent (no shared test data)
✅ Use separate fixtures or cleanup between tests if sharing registries
⚠️ Avoid test interdependencies — they make debugging hard

### Parallel Execution
✅ testcontainers-dotnet is thread-safe for parallel tests
✅ Collection fixtures serialize tests within a collection (fine-grained control)
⚠️ If you see port binding failures, check for container cleanup issues

---

## 8. Sample Integration Test Structure

```csharp
using System.Net.Http.Json;
using Xunit;
using Testcontainers.Registry;

namespace Oras.Tests.Integration.Registry;

[Trait("Category", "Integration")]
[Collection("Registry collection")]
public class RegistryOperationsTests
{
    private readonly RegistryFixture _fixture;

    public RegistryOperationsTests(RegistryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PushArtifact_ReturnsSuccess()
    {
        // Arrange
        var registryUrl = _fixture.RegistryUrl;
        var client = new HttpClient();

        // Act
        var response = await client.PutAsync(
            $"{registryUrl}/v2/test-image/blobs/uploads/abc123",
            new ByteArrayContent(new byte[] { 1, 2, 3 })
        );

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ListRepositories_ReturnsEmptyInitially()
    {
        // Arrange
        var registryUrl = _fixture.RegistryUrl;
        var client = new HttpClient();

        // Act
        var response = await client.GetFromJsonAsync<dynamic>(
            $"{registryUrl}/v2/_catalog"
        );

        // Assert
        Assert.NotNull(response);
    }
}
```

---

## 9. Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Container won't start | Port already in use | Use random port binding (enabled by default) |
| HTTP timeout on registry check | Registry startup slow | Increase wait timeout or check container logs |
| `docker: not found` in CI | Docker not installed | Use `ubuntu-latest` runner (has Docker) |
| Orphaned containers | Tests crashed without cleanup | Run `docker container prune -f` to clean up |
| Port mapping fails | Multiple parallel tests | Use random ports; xUnit parallelizes collection-level |
| Registry connection refused | Container IP/hostname mismatch | Use `container.Hostname` not `localhost` |

---

## 10. Future Enhancements

- [ ] **TLS Support:** Add optional HTTPS testing with self-signed certs
- [ ] **Authentication:** Add htpasswd or token-based auth testing
- [ ] **Multi-Registry Scenarios:** Test interactions between multiple registries
- [ ] **Performance Benchmarks:** Track push/pull latency across releases
- [ ] **Artifact Signing:** OCI image signature verification tests (when ORAS adds support)
- [ ] **Zot Registry Tests:** Evaluate Zot as advanced registry alternative

---

## References

- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [xUnit Fixtures & Shared Context](https://xunit.net/docs/shared-context)
- [OCI Distribution Spec](https://github.com/opencontainers/distribution-spec)
- [Docker Distribution/Registry](https://distribution.github.io/distribution/)
- [GitHub Actions with Testcontainers](https://www.docker.com/blog/running-testcontainers-tests-using-github-actions/)
- [ORAS Project](https://oras.land/)
