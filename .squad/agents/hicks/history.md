# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

### testcontainers-dotnet Patterns (2025-03-06)
- **Package:** `Testcontainers` v3.10.0 in NuGet provides generic container support
- **OCI Registry:** Docker Distribution 3.0.0 (`ghcr.io/distribution/distribution:3.0.0`) is the official image
- **Container Builder:** Use `ContainerBuilder` fluent API from `DotNet.Testcontainers.Builders`
- **Port Binding:** Always use random port binding (`WithPortBinding(port, true)`) to avoid conflicts
- **Wait Strategy:** Use `Wait.ForUnixContainer().UntilHttpRequestIsSucceeded()` with path and port
- **xUnit Integration:** Collection fixtures require exactly ONE public constructor (no overloads)
- **Process Execution:** CliRunner pattern for end-to-end CLI testing via Process.Start()

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

### Sprint 1 Test Infrastructure Setup (S1-12) — 2026-03-06
- **Status:** Test infrastructure complete, main project has compilation errors blocking test implementation
- **Packages Added:** NSubstitute 5.3.0, FluentAssertions 7.0.0, coverlet.collector 6.0.2
- **Test Helpers Created:**
  - `CommandTestHelper` — Invokes System.CommandLine commands, captures stdout/stderr/exit codes
  - `OutputCaptureHelper` — Captures Spectre.Console output for assertions
  - `TestCredentialStore` — In-memory credential store for testing login/logout
- **Tests Implemented:**
  - `TestCredentialStoreTests` — 12 tests for credential store operations
  - `OutputCaptureHelperTests` — 10 tests for output capture helper
  - `TestInfrastructureTests` — 3 smoke tests verifying test framework
- **Directory Structure:** Created Commands/, Services/, Credentials/, Options/, Helpers/ test directories
- **Documentation:** Added comprehensive README.md to test project

### Sprint 1 Integration Test Infrastructure (S1-13) — 2026-03-06
- **Status:** Integration test infrastructure complete and working
- **Registry Fixture:** Implemented using testcontainers-dotnet with Distribution 3.0.0
  - Uses `ghcr.io/distribution/distribution:3.0.0` (NOT registry:2)
  - Random port binding for parallel test execution
  - HTTP health check on `/v2/` endpoint
  - xUnit IAsyncLifetime for proper container lifecycle management
  - Collection fixture pattern for shared registry across test classes
- **CliRunner Helper:** Process-based CLI execution helper
  - Executes compiled oras binary as separate process
  - Captures stdout, stderr, and exit codes
  - Timeout handling (default 30s)
  - Environment variable support
  - Auto-discovery of CLI executable in build output
- **Integration Tests Implemented:**
  - `VersionCommandTests` — 3 smoke tests (version, help, general help) ✓ PASSING
  - `PushPullTests` — 5 tests for push/pull roundtrip, error cases
  - `LoginLogoutTests` — 5 tests for credential store integration
- **Test Organization:**
  - `Integration/` directory with per-command test files
  - `RegistryIntegrationTestBase` for common test functionality
  - Test naming: `MethodName_Scenario_ExpectedBehavior`
  - Trait-based categorization: `[Trait("Category", "Integration")]`
- **Documentation:** Comprehensive Integration/README.md covering infrastructure, patterns, and troubleshooting

### testcontainers-dotnet Patterns (2025-03-06)
- **Problem:** Dallas's code uses System.CommandLine 2.0.0-beta4 but with API patterns from a different version
- **Symptoms:** 
  - `Option<T>` constructor doesn't accept `aliases:` named parameter
  - `Option<T>.Recursive` property doesn't exist
  - `Command.Options.Add()` doesn't work (collection is read-only)
  - `Option<string>.DefaultValueFactory` property missing
  - `Option<string>.AcceptOnlyFromAmong()` method missing
- **Impact:** Main project won't compile, blocking command/option tests
- **Resolution:** Dallas needs to either:
  1. Update code to match System.CommandLine 2.0.0-beta4 API
  2. Switch to stable System.CommandLine 2.0.0 and update code accordingly
- **Workaround Applied:** Fixed `ProgressRenderer.cs` by adding `using Spectre.Console.Rendering;` for `IRenderable` and `RenderOptions` types

### Test Infrastructure Patterns
- **Test Naming:** `MethodName_Scenario_ExpectedBehavior` convention enforced
- **Test Helpers Location:** `test/oras.Tests/Helpers/` with corresponding `*Tests.cs` files
- **Blocked Tests:** Option parsing and command invocation tests cannot be written until main project compiles
- **Ready to Use:** Once Dallas fixes System.CommandLine API issues, test infrastructure is ready for immediate use

### System.CommandLine 2.0.3 Stable API Now Stabilized (Dallas Completion) — 2026-03-06
- **Migration Status:** ✅ Complete. Dallas successfully migrated entire codebase from beta4 incompatible patterns to stable 2.0.3
- **API Patterns Now Locked:** No more breaking changes expected in 2.x releases
- **Test Helper Update Applied:** CommandTestHelper updated to use Console.SetOut/SetError with StringWriter instead of removed TestConsole API
- **Build Status:** ✅ Successful with 0 compilation errors
- **Test Status:** ✅ 15 tests passing
- **Ready for Command Tests:** Full option parsing and command invocation tests can now be written against stable, working codebase

### API Pattern Reference for Writing Tests
1. **Options:** `new Option<T>("--name", "-alias") { Description = "...", DefaultValueFactory = _ => value }`
2. **Arguments:** `new Argument<T>("name") { Description = "...", Arity = ArgumentArity.ZeroOrMore }`
3. **Command Handlers:** `command.SetAction(async (parseResult, cancellationToken) => { ... })`
4. **Value Retrieval:** `parseResult.GetValue(option)` or `parseResult.GetValue(argument)`
5. **Invocation:** `rootCommand.Parse(args).InvokeAsync()`
6. **Global Options:** Set `Recursive = true` property to apply to all subcommands

### Push/Pull Integration Testing Roadmap
- **Blocked on:** OrasProject.Oras v0.5.0 actual API documentation
- **Expected Next:** Once Dallas documents real v0.5.0 API, implement push/pull service tests with mocked callbacks
- **ProgressRenderer Tests:** Can use mocked OnLayerStart/OnLayerProgress/OnLayerComplete callbacks with OutputCaptureHelper assertions


