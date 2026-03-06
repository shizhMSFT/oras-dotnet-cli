# Test Project Codebase Review

**Author:** Hicks (Tester/QA)  
**Date:** 2026-03-06  
**Scope:** Full test project audit — structure, quality, coverage, infrastructure  
**Baseline:** 96 tests (69 passed, 27 skipped, 0 failed)

---

## 1. Current Test Inventory

### Test Counts by Area

| Area | File | Tests | Passed | Skipped |
|------|------|-------|--------|---------|
| Smoke | SmokeTests.cs | 1 | 1 | 0 |
| Infrastructure | TestInfrastructureTests.cs | 3 | 3 | 0 |
| Error Handling | ErrorHandlingTests.cs | 6 | 6 | 0 |
| Commands/Login | LoginCommandTests.cs | 6 | 5 | 1 |
| Commands/Logout | LogoutCommandTests.cs | 5 | 5 | 0 |
| Commands/Push | PushCommandTests.cs | 7 | 7 | 0 |
| Commands/Pull | PullCommandTests.cs | 7 | 7 | 0 |
| Commands/Version | VersionCommandTests.cs | 3 | 3 | 0 |
| Options | OptionParsingTests.cs | 17 | 17 | 0 |
| Helpers | TestCredentialStoreTests.cs | 11 | 11 | 0 |
| Integration/Version | VersionCommandTests.cs | 3 | 3 | 0 |
| Integration/LoginLogout | LoginLogoutTests.cs | 5 | 1 | 4 |
| Integration/PushPull | PushPullTests.cs | 5 | 0 | 5 |
| Integration/Sprint2 | Sprint2CommandTests.cs | 17 | 0 | 17 |
| **TOTALS** | **14 test files** | **96** | **69** | **27** |

### Test Infrastructure

| Component | Status | Notes |
|-----------|--------|-------|
| RegistryFixture | ✅ Working | Distribution 3.0.0 via testcontainers |
| CliRunner | ✅ Working | Process-based CLI execution with timeout |
| CommandTestHelper | ✅ Available | System.CommandLine in-process invocation |
| OutputCaptureHelper | ✅ Available | Spectre.Console TestConsole wrapper |
| TestCredentialStore | ✅ Working | In-memory credential store |
| NSubstitute | ⚠️ Installed but UNUSED | Zero mocks in entire test suite |
| Spectre.Console.Testing | ⚠️ Installed but underused | Only in OutputCaptureHelper |

---

## 2. Coverage Gap Analysis

### Production Code vs Test Coverage Matrix

**Legend:** ✅ = tested, ⚠️ = partially tested, ❌ = no tests, 🔲 = stub (NotImplementedException)

#### Commands (21 files)

| Command | Unit Test | Integration Test | Notes |
|---------|-----------|-----------------|-------|
| VersionCommand | ✅ | ✅ | Good coverage |
| LoginCommand | ✅ | ⚠️ (4/5 skipped) | Parsing tested; auth flow untested |
| LogoutCommand | ✅ | ⚠️ (1/5 passes) | Idempotent logout works |
| PushCommand | ✅ | 🔲 all skipped | Parsing tested; push flow is stub |
| PullCommand | ✅ | 🔲 all skipped | Parsing tested; pull flow is stub |
| AttachCommand | ❌ | 🔲 | No tests at all |
| BackupCommand | ❌ | ❌ | Has real logic (IsArchivePath, FormatSize) |
| RestoreCommand | ❌ | ❌ | Has real validation logic |
| CopyCommand | ❌ | 🔲 skipped | Reference validation untested |
| TagCommand | ❌ | 🔲 skipped | Reference parsing untested |
| ResolveCommand | ❌ | 🔲 skipped | No tests |
| DiscoverCommand | ❌ | 🔲 skipped | No tests |
| RepoLsCommand | ❌ | 🔲 skipped | No tests |
| RepoTagsCommand | ❌ | 🔲 skipped | No tests |
| BlobFetchCommand | ❌ | 🔲 skipped | No tests |
| BlobPushCommand | ❌ | 🔲 skipped | File validation untested |
| BlobDeleteCommand | ❌ | 🔲 skipped | Force flag logic untested |
| ManifestFetchCommand | ❌ | 🔲 skipped | No tests |
| ManifestFetchConfigCommand | ❌ | 🔲 skipped | No tests |
| ManifestPushCommand | ❌ | 🔲 skipped | File validation untested |
| ManifestDeleteCommand | ❌ | 🔲 skipped | Force flag logic untested |

**Gap:** 16 of 21 commands have ZERO unit tests. Only 5 commands have unit test files.

#### Services (9 files)

| Service | Tests | Notes |
|---------|-------|-------|
| ICredentialService | ❌ | Interface — no tests needed |
| CredentialService | ❌ | **Real logic** — validation, best-effort removal |
| IRegistryService | ❌ | Interface |
| RegistryService | 🔲 | Stub — skip for now |
| IPushService | ❌ | Interface |
| PushService | 🔲 | Stub |
| IPullService | ❌ | Interface |
| PullService | 🔲 | Stub |
| ServiceCollectionExtensions | ❌ | DI registration — low risk |

**Gap:** CredentialService has real error handling logic that is untested.

#### Credentials (4 files)

| File | Tests | Notes |
|------|-------|-------|
| DockerConfigStore | ❌ | **CRITICAL** — config load/save, base64 encode, credential priority |
| NativeCredentialHelper | ❌ | **CRITICAL** — process execution, JSON protocol |
| DockerConfig | ❌ | Data model — low priority |
| CredentialJsonContext | ❌ | Source-gen — no logic |

**Gap:** The entire credential subsystem has zero tests. This is the highest-risk gap.

#### Output (4 files)

| File | Tests | Notes |
|------|-------|-------|
| IOutputFormatter | ❌ | Interface |
| TextFormatter | ❌ | ANSI/plain fallback logic, markup escaping |
| JsonFormatter | ❌ | JSON serialization, table-to-dict conversion |
| ProgressRenderer | ❌ | Interactive vs non-interactive, speed calculation |

**Gap:** All output formatting is untested. TextFormatter and JsonFormatter are highly testable.

#### Options (6 files)

| File | Tests | Notes |
|------|-------|-------|
| CommonOptions | ✅ | Covered in OptionParsingTests |
| FormatOptions | ✅ | Covered |
| PackerOptions | ✅ | Covered |
| PlatformOptions | ✅ | Covered |
| RemoteOptions | ✅ | Covered |
| TargetOptions | ✅ | Covered |

**Status:** Good coverage for option existence checks. Missing: invalid value tests, option conflict tests.

#### Tui (5 files)

| File | Tests | Notes |
|------|-------|-------|
| Dashboard | ❌ | ShouldLaunchTui() is easily testable |
| TuiCache | ❌ | **Easily testable** — TTL, invalidation, clear |
| PromptHelper | ❌ | Static wrappers — low priority |
| ManifestInspector | ❌ | Complex TUI — integration test candidate |
| RegistryBrowser | ❌ | Complex TUI — integration test candidate |

**Gap:** TuiCache is a pure unit with zero external dependencies — should have tests.

#### Root Files (4 files)

| File | Tests | Notes |
|------|-------|-------|
| Program.cs | ⚠️ | Smoke test only (--help) |
| ErrorHandler.cs | ❌ | **HIGH PRIORITY** — exit code mapping, debug mode |
| OrasException.cs | ✅ | Exception hierarchy covered |
| CommandExtensions.cs | ❌ | Low complexity |

---

## 3. Test Quality Findings

### 3.1 Strengths

- **Naming convention followed:** `MethodName_Scenario_ExpectedBehavior` used consistently
- **FluentAssertions used consistently:** No mixed assertion styles (`Should()` used throughout)
- **Integration fixture is well-designed:** RegistryFixture with IAsyncLifetime, random port binding, health check
- **Trait-based categorization:** `[Trait("Category", "Integration")]` enables selective test runs
- **Test project configuration is clean:** Proper package references, InternalsVisibleTo

### 3.2 Issues Found

#### A. NSubstitute Never Used (Medium)
- Package is installed but zero mocks exist in the entire test suite
- All command tests use CliRunner (process-based execution) instead of mocking service interfaces
- **Impact:** Cannot unit test command logic in isolation from services
- **Recommendation:** Use NSubstitute to mock ICredentialService, IRegistryService, etc. for command handler unit tests

#### B. Tests Verify Parsing, Not Behavior (Medium)
- Most command unit tests only verify "does it parse without error?" or "does it produce the right exit code?"
- Example: PushCommandTests creates temp files, runs `push`, and checks exit code — but the actual push is a NotImplementedException
- **Impact:** When stubs are implemented, these tests won't catch regressions in real logic
- **Recommendation:** Separate parsing tests from behavior tests; use mocked services for behavior

#### C. Skipped Tests Are Not Actionable (Low)
- 27 of 96 tests are skipped with reasons like "Requires full service implementation"
- These are essentially TODO placeholders, not real tests
- **Impact:** Creates false confidence in test count (96 tests sounds good, but only 69 execute)
- **Recommendation:** Track skipped tests in a backlog; don't count them as coverage

#### D. No Negative Path Testing for Options (Medium)
- OptionParsingTests verify options exist with correct aliases and defaults
- Missing: invalid values (negative concurrency, unknown format), conflicting options (--password + --password-stdin), boundary conditions
- **Recommendation:** Add `[Theory]` tests with `[InlineData]` for invalid option values

#### E. Temp File Cleanup (Low)
- `RegistryIntegrationTestBase.CreateTestFileAsync()` creates files under `Path.GetTempPath()/oras-tests/`
- No automatic cleanup in `DisposeAsync()`
- Some tests (PushCommandTests) use try/finally for cleanup, but pattern is inconsistent
- **Recommendation:** Track created paths and delete in DisposeAsync()

#### F. Cross-Platform Concerns (Low, but flagged)
- `CliRunner.EscapeArgument()` uses Windows-style quote escaping on all platforms
- CliRunner correctly detects .exe vs no-extension for executable discovery
- Path.Combine used correctly (no hardcoded separators)
- **No line ending issues found** — tests don't assert on exact output formatting

### 3.3 Potential Bug in Production Code (Found During Review)

- **TextFormatter.SupportsInteractivity** (line 19): Returns `!_console.Profile.Capabilities.Interactive` — this is INVERTED. It reports non-interactive when the console IS interactive. Likely a bug (negation should be removed). Needs verification.

---

## 4. Prioritized Tests to Add During Refactoring

### Tier 1: Critical (Add Before Any Refactoring)

| # | Target | Type | Tests Needed | Rationale |
|---|--------|------|-------------|-----------|
| 1 | **ErrorHandler** | Unit | 6-8 tests | Every command routes through this. Exit code mapping (1 vs 2), debug mode, exception type routing, recommendation display |
| 2 | **DockerConfigStore** | Unit | 10-12 tests | Credential storage is security-critical. Load/save, base64 encoding, credential lookup priority, graceful error on corrupt config, ListRegistriesAsync deduplication |
| 3 | **NativeCredentialHelper** | Unit | 6-8 tests | External process execution. Helper name prefixing, JSON protocol, exit code handling, graceful failure on missing helper |
| 4 | **CredentialService** | Unit | 4-6 tests | Service orchestration. Validation flow (returns false on error), best-effort removal, delegation to config store |

### Tier 2: Important (Add During Feature Work)

| # | Target | Type | Tests Needed | Rationale |
|---|--------|------|-------------|-----------|
| 5 | **JsonFormatter** | Unit | 6-8 tests | Machine-readable output must be correct. WriteStatus structure, WriteError with/without recommendation, WriteTable conversion, ConvertTreeToJson |
| 6 | **TextFormatter** | Unit | 8-10 tests | User-facing output. ANSI vs plain fallback for every method, markup escaping with special chars, WriteJson pretty-print, bug verification (SupportsInteractivity) |
| 7 | **ProgressRenderer** | Unit | 6-8 tests | UX quality. Interactive vs redirected detection, layer progress tracking, size formatting (bytes→KB→MB→GB), speed calculation |
| 8 | **TuiCache** | Unit | 8-10 tests | Pure logic, easy to test. Set/Get, TTL expiration, expired entry removal, pattern invalidation (case insensitive), Clear, generic types |
| 9 | **BackupCommand** | Unit | 4-6 tests | Has real logic. IsArchivePath (.tar/.tar.gz), FormatSize, directory validation |
| 10 | **CopyCommand** | Unit | 3-4 tests | Reference validation (must contain '/'), parameter extraction |

### Tier 3: Nice to Have (Add When Commands Are Implemented)

| # | Target | Type | Tests Needed | Rationale |
|---|--------|------|-------------|-----------|
| 11 | Option conflict tests | Unit | 5-8 tests | --password + --password-stdin, negative concurrency, invalid platform format |
| 12 | Remaining 16 commands | Unit | 2-3 per command | Argument parsing, required argument validation |
| 13 | Dashboard.ShouldLaunchTui | Unit | 3-4 tests | TTY detection, args detection, env var override |
| 14 | ServiceCollectionExtensions | Unit | 1-2 tests | DI registration verification |
| 15 | FormatOptions.CreateFormatter | Unit | 2-3 tests | Factory returns correct type for "text" vs "json" |

### Estimated New Test Count

| Tier | New Tests | Running Total |
|------|-----------|---------------|
| Current passing | — | 69 |
| Tier 1 | 26-34 | ~100 |
| Tier 2 | 35-46 | ~140 |
| Tier 3 | 25-35 | ~170 |

---

## 5. Test Infrastructure Improvements

### 5.1 Testcontainers Setup — Verdict: Solid

- RegistryFixture uses `ghcr.io/distribution/distribution:3.0.0` ✅
- Random port binding prevents parallel test conflicts ✅
- HTTP health check on `/v2/` before tests run ✅
- Collection fixture pattern shares container across test classes ✅

**One concern:** No graceful skip when Docker is unavailable. Tests will crash in CI without Docker. Recommend adding a `DockerAvailableAttribute` or skip-on-missing-Docker pattern.

### 5.2 CliRunner — Verdict: Good but Needs Enhancement

- Process execution with stdout/stderr capture ✅
- Timeout handling (30s default) ✅
- Executable auto-discovery ✅
- **Missing:** No way to inject environment variables for ORAS_DEBUG testing
- **Missing:** No way to provide stdin input for interactive command testing

### 5.3 Recommendations

1. **Start using NSubstitute** — Mock ICredentialService, IRegistryService for isolated command unit tests
2. **Add Docker availability check** — Skip integration tests gracefully when Docker is unavailable
3. **Add temp file tracking to RegistryIntegrationTestBase** — Auto-cleanup in DisposeAsync
4. **Create assertion helpers** — `ShouldContainError()`, `ShouldHaveExitCode()` to reduce brittle string matching
5. **Add environment variable support to CliRunner** — Needed for ORAS_DEBUG and credential helper testing
6. **Consider coverlet configuration** — Enable code coverage reporting in CI to track actual line/branch coverage

---

## 6. Summary

The test project has good infrastructure bones (testcontainers, CliRunner, fixture patterns) but significant coverage gaps. Of ~54 production files, only ~12 have any test coverage. The credential subsystem (DockerConfigStore, NativeCredentialHelper) and output formatters have zero tests despite containing critical, complex logic. The 27 skipped tests are all placeholder stubs waiting for service implementations.

**Immediate action items:**
1. Write ErrorHandler unit tests (every command depends on this)
2. Write DockerConfigStore + NativeCredentialHelper unit tests (security-critical)
3. Write JsonFormatter + TextFormatter unit tests (all user-visible output)
4. Start using NSubstitute for service mocking
5. Verify TextFormatter.SupportsInteractivity bug (inverted boolean)
