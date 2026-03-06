# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-06: Project Structure & SDK Setup
- **SDK**: .NET 10 SDK (10.0.103) is installed and working. Project configured with `net10.0` TFM.
- **Project Structure**:
  - `src/oras/oras.csproj` — CLI app (console, net10.0)
  - `test/oras.Tests/oras.Tests.csproj` — test project (xUnit)
  - Solution file: `oras.sln` at root
- **Key Dependencies**:
  - System.CommandLine (2.0.0-beta4.22272.1) — per Ripley's ADR-001 for command parsing
  - Spectre.Console (0.50.0) — rendering only; not CLI parsing (ADR-001)
  - OrasProject.Oras (0.5.0) — Note: Latest available is 0.5.0, not 1.2.0 as initially planned
  - xUnit (2.9.3), Testcontainers (3.10.0)
- **Configuration Files**:
  - `global.json` — pins SDK to 10.0.103
  - `.editorconfig` — C# conventions (4-space indent, file-scoped namespaces)
  - `Directory.Build.props` — shared properties (LangVersion=preview, Nullable=enable, TreatWarningsAsErrors=true)
- **Code Style Enforcement**: 
  - AnalysisLevel=latest-all enforces strict analyzer rules per ADR-006 (AOT readiness)
  - Must use `ConfigureAwait(false)` in non-test async code
  - Test methods cannot use underscores in names (CA1707)
  - Internal types require InternalsVisibleTo for test access

### 2026-03-06: OrasProject.Oras v0.5.0 Constraint & Architecture Alignment

**Critical Finding:** OrasProject.Oras latest available is v0.5.0 (not v1.2.0). Documented in Active Decision: "OrasProject.Oras Package Version".

**Architectural Implications (from Ripley's review):**
- ADR-003 (Credential Store): Library has no credential *store*, only ICredentialProvider interface. CLI must implement Docker config.json store + credential helper protocol.
- ADR-005 (Defer OCI Layout): Library has no OCI layout store. Phase 1 focuses on remote registries only; `--oci-layout` deferred.
- ADR-009 (Phase 1 Scope): v0.5.0 supports 10/12 commands per Dallas's research. Credential storage (login/logout) and version command are CLI-level responsibility.

**Team Decision Pending:** Whether v0.5.0 API surface meets all Phase 1 requirements or if contributions/adjustments needed.
