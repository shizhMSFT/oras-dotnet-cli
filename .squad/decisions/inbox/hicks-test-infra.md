# Test Infrastructure Setup Complete (S1-12)

**Date:** 2026-03-06  
**Author:** Hicks (Tester)  
**Status:** Partially Complete — Blocked by Compilation Errors

## Summary

Implemented Sprint 1 test infrastructure (S1-12) including:
- Test project configuration with all required dependencies
- Three test helper classes (CommandTestHelper, OutputCaptureHelper, TestCredentialStore)
- Test directory structure (Commands/, Services/, Credentials/, Options/, Helpers/)
- Helper tests to verify test infrastructure works
- Documentation (README.md)

## Blocking Issue

**Main CLI project has System.CommandLine API compatibility issues** preventing tests from compiling:

### Errors Found:
1. `Option<T>` constructor doesn't accept `aliases:` named parameter
2. `Option<T>.Recursive` property doesn't exist
3. `Command.Options` is read-only, can't use `.Add()`
4. `Option<string>.DefaultValueFactory` property doesn't exist
5. `Option<string>.AcceptOnlyFromAmong()` method doesn't exist

These appear to be mismatches between System.CommandLine 2.0.0-beta4 API and the code written for a different version.

### Affected Files:
- `src/Oras.Cli/Options/CommonOptions.cs`
- `src/Oras.Cli/Options/RemoteOptions.cs`
- `src/Oras.Cli/Options/FormatOptions.cs`
- `src/Oras.Cli/Options/TargetOptions.cs`
- `src/Oras.Cli/Options/PackerOptions.cs`
- `src/Oras.Cli/Options/PlatformOptions.cs`
- `src/Oras.Cli/Output/ProgressRenderer.cs` (fixed — added `using Spectre.Console.Rendering;`)

## What Was Delivered

### 1. Test Project Configuration
- ✅ Added NSubstitute 5.3.0 for mocking
- ✅ Added FluentAssertions 7.0.0 for readable assertions
- ✅ Added coverlet.collector 6.0.2 for code coverage
- ✅ Updated project reference to `src/Oras.Cli/oras.csproj`
- ✅ Added versions to `Directory.Packages.props` for central management

### 2. Test Helpers Created

#### CommandTestHelper
- Invokes System.CommandLine commands programmatically
- Captures stdout, stderr, and exit codes
- Provides `InvokeAsync()` methods for `Command` and `RootCommand`
- `Reset()` method to clear between tests

#### OutputCaptureHelper
- Wraps Spectre.Console TestConsole
- Provides `Contains()` and `AnyLineMatches()` for assertions
- Exposes `Console` property for passing to components
- `Clear()` method to reset output

#### TestCredentialStore
- In-memory credential storage for testing
- `Store()`, `Get()`, `Remove()`, `Clear()`, `Contains()` methods
- Prevents modification of real Docker config during tests
- Validation for null/empty registry names

### 3. Test Organization
Created directory structure:
- `Commands/` — command tests (empty, waiting for compilation fix)
- `Services/` — service layer tests (empty, waiting for implementation)
- `Credentials/` — credential store tests (empty, waiting for implementation)
- `Options/` — option parsing tests (empty, waiting for compilation fix)
- `Helpers/` — test helper classes and their tests
- `Integration/` — integration tests (RegistryCollection already exists)
- `Fixtures/` — test fixtures (RegistryFixture already exists)

### 4. Tests Implemented
- ✅ `TestCredentialStoreTests` — 12 tests covering Store, Get, Remove, Clear, validation
- ✅ `OutputCaptureHelperTests` — 10 tests covering output capture and assertions
- ✅ `TestInfrastructureTests` — 3 smoke tests verifying test framework works

### 5. Documentation
- ✅ `test/oras.Tests/README.md` — Complete documentation of test infrastructure, helpers, conventions, and current status

## What's Missing (Blocked)

Cannot implement until Dallas fixes System.CommandLine API issues:
- ❌ Option parsing tests (CommonOptions, RemoteOptions, FormatOptions, etc.)
- ❌ Command invocation tests (version, login, logout, push, pull)
- ❌ Error handling tests (exception → error message → exit code mapping)
- ❌ Credential store file I/O tests (DockerConfigStore read/write)

## Recommendation for Dallas

Replace System.CommandLine 2.0.0-beta4 with the stable 2.0.0 release, or update the Options code to match the beta4 API:

```csharp
// OLD (doesn't work with beta4):
new Option<bool>(name: "--debug", aliases: ["-d"], ...)
    { Recursive = true }

// NEW (beta4 compatible):
new Option<bool>("--debug", "-d", ...)  // Positional params, no 'aliases:'
    .AddAlias("-d")                      // Use AddAlias() instead
// (No Recursive property exists in beta4)

// For Command.Options:
// OLD: command.Options.Add(option)
// NEW: command.AddOption(option)
```

Alternatively, pin to System.CommandLine 2.0.0-beta4.22272.1 and find example code that matches that specific beta API.

## Testing Strategy Once Unblocked

1. **Unit Tests First:**
   - Option parsing: verify flags parse correctly
   - Command binding: verify arguments bind to handlers
   - Mock services in command handlers
   - Error translation: library exceptions → user messages

2. **Integration Tests Second:**
   - Use existing RegistryFixture
   - Full command pipeline tests with real registry
   - Push/pull roundtrip verification

3. **Coverage Target:**
   - Aim for >80% code coverage on Commands/, Options/, Services/
   - Use `dotnet test --collect:"XPlat Code Coverage"`

## Decision

**Keep test infrastructure as-is.** It's ready to use once the main project compiles.

Do NOT attempt to fix Dallas's code — that's outside Hicks's scope. Document the issue clearly for Dallas to address.

Once Dallas resolves the System.CommandLine API issues, Hicks can immediately write comprehensive unit tests using the infrastructure built here.
