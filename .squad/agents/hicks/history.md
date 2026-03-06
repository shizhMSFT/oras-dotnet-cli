# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

### testcontainers-dotnet Patterns (2025-03-06)
- **Package:** `Testcontainers` v3.10.0 in NuGet provides generic container support (no separate `.Generic` package)
- **OCI Registry:** Docker Distribution (`registry:2.8.3`) is recommended for integration tests — OCI compliant, lightweight, starts in ~1-2s
- **Port Binding:** Always use random port binding (`WithPortBinding(port, assignRandomHostPort: true)`) to avoid conflicts in parallel test execution
- **Wait Strategy:** Use `Wait.ForUnixContainer().UntilHttpRequestIsSucceeded()` with `/v2/` health check endpoint (registry-specific)
- **Container Types:** No direct `ContainerBuilder` type in Testcontainers.NET base package; use reflection or project-specific wrappers for now

### xUnit Fixture Patterns
- **IAsyncLifetime:** Required for async container startup/cleanup (replaces `IDisposable` for async work)
- **Class Fixtures:** Use `IClassFixture<T>` for per-test-class container instances (good for isolated test groups)
- **Collection Fixtures:** Use `[CollectionDefinition]` + `[Collection]` attributes to share expensive setup across multiple test classes
- **Lifecycle Order:** xUnit creates fixture instance → calls `InitializeAsync()` → runs tests → calls `DisposeAsync()`
- **Lifetime:** Class fixtures live per-test-class, collection fixtures live across all classes in a collection

### GitHub Actions + Docker + Testcontainers
- **No DinD setup needed:** Ubuntu runners (`ubuntu-latest`) have Docker pre-installed; testcontainers-dotnet auto-detects it
- **Parallel Test Execution:** xUnit parallelizes tests within same collection; collection fixtures serialize tests (fine-grained parallelism control)
- **CI Optimization:** For faster CI, consider Testcontainers Cloud (offload container workload) or service containers for standard dependencies
- **Test Categorization:** Use `[Trait("Category", "Integration")]` to separate unit and integration tests for filtering in CI

### Project Structure Best Practice
- **Unit Tests:** `test/oras.Tests/Unit/**/*.cs` — no Docker, fast, no fixtures
- **Integration Tests:** `test/oras.Tests/Integration/**/*.cs` — testcontainers fixtures, shared registry
- **Fixtures:** `test/oras.Tests/Fixtures/` — reusable container lifecycle management
- **Smoke Tests:** Already exist in `test/oras.Tests/SmokeTests.cs` — basic CLI health checks

### Code Analysis Alignment
- Project uses `TreatWarningsAsErrors=true` with `AnalysisLevel=latest-all`
- `IAsyncLifetime` must be implemented; `InitializeAsync()` + `DisposeAsync()` both required
- URI usage preferred over strings for HTTP endpoints (CA2234)
- All awaits should use `.ConfigureAwait(false)` for library code (CA2007)
- Collection definition classes must be `public` for xUnit discovery (xUnit1027)
