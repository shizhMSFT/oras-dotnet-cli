# Integration Tests

This directory contains integration tests for the oras CLI that test the complete system including:
- CLI process execution
- OCI registry interactions using Docker Distribution 3.0.0
- Real network operations (push, pull, login, logout)

## Test Infrastructure

### Registry Fixture

**Location:** `Fixtures/RegistryFixture.cs`

The `RegistryFixture` manages the lifecycle of a containerized OCI-compliant registry using testcontainers-dotnet. It:
- Uses `ghcr.io/distribution/distribution:3.0.0` (Docker Distribution v3)
- Starts the registry on a random port to avoid conflicts
- Waits for the registry to be healthy (HTTP 200 on `/v2/`)
- Provides helper methods for querying the registry
- Automatically cleans up after tests

**Collection Fixture:** Tests that need a shared registry should use the `[Collection("Registry collection")]` attribute to share a single registry container across multiple test classes.

### CLI Runner

**Location:** `Helpers/CliRunner.cs`

The `CliRunner` executes the compiled oras CLI as a separate process and captures:
- Standard output
- Standard error
- Exit code

This allows testing the CLI exactly as users will run it, including:
- Command-line argument parsing
- Output formatting
- Error handling
- Exit code conventions

### Base Test Class

**Location:** `Integration/RegistryIntegrationTestBase.cs`

The `RegistryIntegrationTestBase` provides common functionality for integration tests:
- Access to the shared registry fixture
- Helper methods for creating test repositories and files
- Utilities for building registry references
- Cleanup helpers

## Test Organization

### By Command

Integration tests are organized by command in separate files:
- `VersionCommandTests.cs` — Smoke tests for version and help
- `PushPullTests.cs` — Push and pull roundtrip tests
- `LoginLogoutTests.cs` — Credential store integration tests

### Test Naming Convention

All integration tests follow the naming pattern:
```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `PushPull_SingleFile_RoundtripSucceeds`
- `Push_ToNonexistentRegistry_Fails`
- `Login_WithValidCredentials_Succeeds`

### Test Categories

Tests are marked with traits for filtering:
- `[Trait("Category", "Integration")]` — All integration tests
- `[Trait("Category", "SkipIfNoCredentialStore")]` — Tests requiring credential store (may skip on CI)

## Running Integration Tests

### Prerequisites

1. **Docker must be running** — Testcontainers requires Docker to start registry containers
2. **CLI must be built** — Run `dotnet build` in the solution root first

### Run All Tests

```bash
dotnet test
```

### Run Only Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

### Skip Credential Store Tests

On systems without Docker credential helper support:
```bash
dotnet test --filter "Category!=SkipIfNoCredentialStore"
```

## CI/CD Considerations

### GitHub Actions

Ubuntu runners have Docker pre-installed. No special setup is needed:

```yaml
- name: Run integration tests
  run: dotnet test --filter "Category=Integration"
```

### Docker Availability

If Docker is not available, integration tests will fail during container startup. Consider:
1. Using test skip conditions based on Docker availability
2. Separating unit and integration test runs in CI
3. Using Testcontainers Cloud for remote container execution

## Exit Code Conventions

Integration tests verify exit codes follow the team decisions (DEC-PRD-006):
- **0** — Success
- **1** — General error
- **2** — Argument/parsing error

## Test Data Management

### Temporary Files

Tests create temporary files in `Path.GetTempPath()/oras-tests/`. Each test gets a unique subdirectory identified by GUID.

**Cleanup:** Tests are responsible for cleaning up their temporary files in finally blocks.

### Registry Isolation

Each test method should use `GetTestRepository()` to get a unique repository name. This prevents tests from interfering with each other.

Repository naming pattern: `test-{testclassname}-{timestamp}`

## Common Test Patterns

### Push/Pull Roundtrip

```csharp
[Fact]
public async Task PushPull_SingleFile_RoundtripSucceeds()
{
    var repository = GetTestRepository();
    var reference = GetRegistryReference(repository);
    var file = await CreateTestFileAsync("content");
    var pullDir = CreateTempDirectory();
    
    var pushResult = await Cli.ExecuteAsync($"push {reference} {file}");
    pushResult.ExitCode.Should().Be(0);
    
    var pullResult = await Cli.ExecuteAsync($"pull {reference} -o {pullDir}");
    pullResult.ExitCode.Should().Be(0);
}
```

### Error Case Testing

```csharp
[Fact]
public async Task Push_ToNonexistentRegistry_Fails()
{
    var result = await Cli.ExecuteAsync($"push invalid:5000/repo:tag file.txt");
    result.ExitCode.Should().NotBe(0);
    result.StandardError.Should().NotBeNullOrEmpty();
}
```

## Architecture Decisions

### Why testcontainers-dotnet?

- **Real Environment:** Tests run against an actual OCI registry, not mocks
- **Isolation:** Each test run gets a fresh registry on a random port
- **CI-Friendly:** Works on GitHub Actions without special configuration
- **Deterministic:** Container lifecycle is managed by xUnit fixtures

### Why Process Execution (CliRunner)?

- **End-to-End:** Tests the actual compiled binary users will run
- **Complete Coverage:** Tests argument parsing, output formatting, exit codes
- **Realistic:** Catches issues with process lifecycle, environment variables, etc.

### Why Distribution 3.0.0?

- **OCI Compliant:** Full OCI Distribution Spec support
- **Official:** Recommended by OCI community
- **Maintained:** Actively developed and supported
- **Fast:** Starts in 1-2 seconds for tests

## Future Enhancements

Potential improvements for integration test infrastructure:

1. **Authenticated Registry:** Add tests for registries requiring authentication
2. **TLS Support:** Test with HTTPS registries using self-signed certificates
3. **Content Verification:** Add checksums/digest verification helpers
4. **Performance Tests:** Measure push/pull times for regression detection
5. **Parallel Execution:** Optimize for faster test runs with isolated registries
6. **Registry Variants:** Test against Zot, Harbor, or other OCI registries

## Troubleshooting

### Container Startup Failures

**Symptom:** Tests fail with "Failed to start container"

**Solutions:**
1. Ensure Docker is running
2. Check Docker has permission to pull images
3. Verify network connectivity to `ghcr.io`

### CLI Not Found

**Symptom:** Tests fail with "Could not find oras CLI executable"

**Solutions:**
1. Build the CLI project: `dotnet build src/Oras.Cli/oras.csproj`
2. Ensure build succeeded without errors
3. Check build output directory contains the executable

### Port Conflicts

**Symptom:** Tests fail with port binding errors

**Solution:** The fixture uses random port assignment, so this should be rare. If it occurs, retry the test.

## References

- [testcontainers-dotnet Documentation](https://dotnet.testcontainers.org/)
- [xUnit Documentation](https://xunit.net/)
- [OCI Distribution Spec](https://github.com/opencontainers/distribution-spec)
- [Docker Distribution](https://github.com/distribution/distribution)
