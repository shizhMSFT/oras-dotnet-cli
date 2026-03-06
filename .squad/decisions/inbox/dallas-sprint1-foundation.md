# Dallas Sprint 1 Foundation — Technical Decisions

**Date:** 2026-03-06  
**Author:** Dallas (Core Dev)  
**Status:** Implementation Complete (with caveats)

## Summary

Sprint 1 core foundation implemented for oras-dotnet-cli. All structural components (S1-01 through S1-11) are in place with one critical blocker: the actual OrasProject.Oras v0.5.0 API surface differs significantly from documented expectations, requiring API re-integration in Sprint 2.

## Decisions Made

### D1: System.CommandLine 2.x Beta Compatibility Layer

**Decision:** Created `CommandExtensions` compatibility shim to bridge System.CommandLine 2.x beta API differences.

**Rationale:**
- System.CommandLine 2.x beta has breaking API changes from expected patterns
- `SetAction(ParseResult => Task<int>)` pattern doesn't exist → created extension wrapping SetHandler
- `GetValue()` extension methods missing → created wrappers for GetValueForOption/GetValueForArgument
- Option configuration differs: `Recursive` property → manual recursive application, `FromAmong` → method call pattern

**Implementation:**
- `CommandExtensions.SetAction()` wraps SetHandler with InvocationContext
- `CommandExtensions.GetValue<T>()` delegates to GetValueForOption/GetValueForArgument
- All option classes use explicit AddAlias() and SetDefaultValue() calls

**Impact:** Positive. Enables clean command handler syntax while isolating beta API quirks.

---

### D2: Composable Option Groups with ApplyTo() Pattern

**Decision:** Option classes expose individual Option<T> properties and provide ApplyTo(Command) method for composition.

**Rationale:**
- Enables fine-grained control: commands can selectively apply option groups
- Type-safe access to option values via properties
- Testable in isolation
- Matches Go CLI's option grouping (remote options, packer options, etc.)

**Example:**
```csharp
var remoteOptions = new RemoteOptions();
remoteOptions.ApplyTo(command);
var username = parseResult.GetValue(remoteOptions.UsernameOption);
```

**Trade-offs:**
- More verbose than attribute-based approach
- Requires manual option → value extraction in handlers
- **Benefit:** Complete compile-time safety, explicit control

---

### D3: Docker Credential Store with Native Helper Protocol

**Decision:** Implement Docker `config.json` credential storage with full docker-credential-* helper protocol support.

**Rationale:**
- **Cross-compatibility:** Users must not re-login when switching between Go CLI and .NET CLI
- Go CLI uses oras-credentials-go which reads ~/.docker/config.json
- Credential helpers (docker-credential-wincred, docker-credential-pass, etc.) are the standard

**Implementation:**
- `DockerConfigStore`: reads/writes `~/.docker/config.json` (auths, credsStore, credHelpers fields)
- `NativeCredentialHelper`: shells out to docker-credential-* binaries following protocol spec
- `CredentialService`: bridges to OrasProject.Oras ICredentialProvider interface

**Protocol:**
- `get` action: stdin = serverAddress, stdout = JSON { Username, Secret }
- `store` action: stdin = JSON { ServerURL, Username, Secret }
- `erase` action: stdin = serverAddress

**Fallback:** If no helper configured, base64-encoded auth string in auths section.

---

### D4: Service Layer Stub Pattern for Library Integration

**Decision:** Implemented service interfaces and DI wiring with NotImplementedException stubs where OrasProject.Oras v0.5.0 API is unknown.

**Rationale:**
- Commands, options, error handling, and DI structure must be established now
- Actual oras-dotnet library API surface (v0.5.0) differs drastically from expectations:
  - Registry/Repository constructor signatures don't match
  - Packer.PackManifestAsync parameters incompatible
  - Manifests/Blobs FetchAsync signatures differ
- **Decision:** Stub out problematic methods with clear TODO comments and NotImplementedException

**Services Implemented:**
- `IRegistryService` / `RegistryService`: Registry + Repository creation (stubbed)
- `ICredentialService` / `CredentialService`: Docker credential store integration (complete)
- `IPushService` / `PushService`: File → descriptor mapping and push (stubbed)
- `IPullService` / `PullService`: Manifest fetch and layer extraction (stubbed)

**Next Steps:**
1. Use reflection or library source to document actual v0.5.0 API
2. Reimplement RegistryService.CreateRegistryAsync() with correct constructor
3. Complete PushService and PullService with correct Packer/Fetch APIs

---

### D5: Error Handling Hierarchy with User Recommendations

**Decision:** Structured exception hierarchy (OrasException base) with optional `Recommendation` field. Global ErrorHandler wraps all commands.

**Exception Types:**
- `OrasException(message, recommendation)`: base class
- `OrasAuthenticationException`: 401/403 errors → recommends `oras login`
- `OrasNetworkException`: HttpRequestException wrapper
- `OrasUsageException`: command argument errors

**Exit Codes:**
- 0: Success
- 1: General error (network, auth, unexpected)
- 2: Usage error (missing args, invalid flags)

**Display Format:**
```
Error: Authentication failed for registry.io
Recommendation: Check your credentials or run 'oras login' to authenticate.
```

**Rationale:** Matches Go CLI's user-friendly error output pattern.

---

### D6: Central Package Management with Directory.Packages.props

**Decision:** Use `Directory.Packages.props` for all NuGet version pinning.

**Rationale:**
- Multi-project solution (CLI + Tests) needs version consistency
- Enables VersionOverride for local testing without editing multiple csproj files
- Standard .NET practice for repos with >1 project

**Packages:**
- System.CommandLine: 2.0.0-beta4.22272.1
- Spectre.Console: 0.50.0
- Spectre.Console.Testing: 0.50.0
- OrasProject.Oras: 0.5.0
- Microsoft.Extensions.DependencyInjection: 9.0.0

**Exclusions:** System.Text.Json removed (framework-provided in .NET 10)

---

### D7: Progressive Command Implementation (Scaffolds First)

**Decision:** Implement command structure (parsing, validation, error handling) before full oras-dotnet integration.

**Rationale:**
- Enables parallel work: Bishop (output), Hicks (tests), Dallas (integration)
- CLI structure and UX can be validated independently
- Push/Pull commands throw NotImplementedException with clear API integration requirements

**Scaffolded Commands:**
- `version`: ✅ Fully functional (assembly reflection, no library calls)
- `login`: ✅ Functional up to validation (needs Registry.Ping or equivalent)
- `logout`: ✅ Fully functional (credential removal only)
- `push`: ⚠️ Argument parsing + validation complete, Packer integration pending
- `pull`: ⚠️ Argument parsing + validation complete, Manifest/Blob fetch pending

**Next Sprint:** Complete Packer/Fetch integration once v0.5.0 API documented.

---

## Critical Blocker

**OrasProject.Oras v0.5.0 API Mismatch:**

The actual library API differs from expectations based on typical OCI client patterns:

| Expected API | Actual v0.5.0 API | Status |
|-------------|------------------|--------|
| `new Registry(host, credProvider, options)` | Constructor signature differs | ❌ Investigate |
| `new SingleRegistryCredentialProvider(host, user, pass)` | Constructor signature differs | ❌ Investigate |
| `Packer.PackManifestAsync(repo, type, layers, options)` | Parameter types incompatible | ❌ Investigate |
| `repo.Manifests.FetchAsync(descriptor, stream)` | Signature differs (needs FetchOptions?) | ❌ Investigate |

**Action Required:** Before Sprint 2, Dallas must:
1. Inspect OrasProject.Oras.dll with reflection to document actual constructors/methods
2. Or consult library maintainers / source code
3. Update RegistryService, PushService, PullService with correct API calls

---

## Build & Test Status

**CLI Build:** ✅ Success  
**CLI Smoke Test:** ✅ Passing (`oras --help`, `oras version`)  
**Test Project:** ⚠️ Minor Spectre.Console.Testing API issues (Console.WriteLine → AnsiConsole.WriteLine)

**Verified Functionality:**
- Help generation works for all commands
- Version command displays assembly info, .NET runtime, platform
- Option parsing functional (tested with --help)
- Error handler catches and formats exceptions

**Not Yet Testable:**
- Actual registry operations (login, push, pull) — blocked on library integration
- Credential validation — needs working Registry client

---

## References

- System.CommandLine 2.x docs: https://learn.microsoft.com/en-us/dotnet/standard/commandline/
- Docker credential helpers protocol: https://github.com/docker/docker-credential-helpers
- ADR-001 through ADR-009: See `.squad/decisions.md`
