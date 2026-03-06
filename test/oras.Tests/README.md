# ORAS .NET CLI Test Infrastructure

This directory contains the test infrastructure for the ORAS .NET CLI project.

## Test Project Setup

The test project (`oras.Tests.csproj`) is configured with:

- **xUnit 2.9.3** — Test framework
- **xunit.runner.visualstudio** — Visual Studio test runner integration
- **NSubstitute 5.3.0** — Mocking library for creating test doubles
- **FluentAssertions 7.0.0** — Readable assertion library
- **Testcontainers 3.10.0** — Container lifecycle management for integration tests
- **coverlet.collector 6.0.2** — Code coverage collection

## Directory Structure

```
test/oras.Tests/
├── Helpers/                   # Test helper classes
│   ├── CommandTestHelper.cs          # System.CommandLine command invocation helper
│   ├── OutputCaptureHelper.cs        # Spectre.Console output capture helper
│   ├── TestCredentialStore.cs        # In-memory credential store for testing
│   ├── TestCredentialStoreTests.cs   # Tests for TestCredentialStore
│   └── OutputCaptureHelperTests.cs   # Tests for OutputCaptureHelper
├── Commands/                  # Command tests (to be implemented)
├── Services/                  # Service layer tests (to be implemented)
├── Credentials/               # Credential store tests (to be implemented)
├── Options/                   # Option parsing tests (to be implemented)
├── Integration/               # Integration tests with testcontainers
│   └── RegistryCollection.cs         # Shared registry fixture collection
├── Fixtures/                  # Test fixtures for integration tests
│   └── RegistryFixture.cs            # OCI registry container fixture
├── SmokeTests.cs              # Basic smoke tests
└── TestInfrastructureTests.cs # Tests for test infrastructure itself
```

## Test Helpers

### CommandTestHelper

Helper for testing System.CommandLine commands programmatically:

```csharp
var helper = new CommandTestHelper();
await helper.InvokeAsync(command, "--help");

helper.ExitCode.Should().Be(0);
helper.StandardOutput.Should().Contain("usage");
```

### OutputCaptureHelper

Helper for capturing and asserting Spectre.Console output:

```csharp
var helper = new OutputCaptureHelper();
helper.Console.WriteLine("test output");

helper.Contains("test").Should().BeTrue();
helper.Lines.Should().HaveCount(1);
```

### TestCredentialStore

In-memory credential store for testing login/logout without modifying real Docker config:

```csharp
var store = new TestCredentialStore();
store.Store("registry.example.com", "user", "pass");

var cred = store.Get("registry.example.com");
cred.Should().NotBeNull();
cred.Username.Should().Be("user");
```

## Test Naming Convention

All tests follow the pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `Store_AddsCredential`
- `Get_ReturnsNull_WhenCredentialDoesNotExist`
- `RootCommand_WithHelpFlag_ReturnsZeroExitCode`

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Current Status

✅ Test project setup complete
✅ Test helpers implemented and tested
✅ Directory structure created
✅ Central package management configured
✅ Code coverage collection enabled

⚠️ **Blocked:** Main CLI project has compilation errors that need to be fixed before command/option tests can be written.
   - System.CommandLine API mismatches (Option.Recursive, Option.aliases, etc.)
   - Once Dallas fixes the main project compilation, command and option tests can be uncommented/implemented

## Next Steps

1. Wait for Dallas to fix System.CommandLine API compatibility issues in main project
2. Implement command tests once compilation succeeds
3. Implement option parsing tests
4. Create service layer mocks and tests
5. Implement credential store file I/O tests
6. Add error handling tests

## Integration Tests

Integration tests use testcontainers-dotnet with Docker Distribution registry (registry:2.8.3).
See `Fixtures/RegistryFixture.cs` for the container lifecycle management.

The `RegistryCollection` allows multiple test classes to share a single registry container instance.
