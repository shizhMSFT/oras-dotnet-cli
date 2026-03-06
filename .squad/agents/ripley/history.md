# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-06 — Architecture Design Review

**Go CLI command tree**: 15 top-level commands + 3 subcommand groups (blob, manifest, repo). Manifest has nested `index` subgroup. Experimental: backup, restore. All commands use composable option structs (Common, Remote, Target, Packer, Format, Platform, Terminal).

**oras-dotnet API surface**: Core interfaces are `ITarget`/`IReadOnlyTarget` with `IRepository` as the main remote implementation. Key operations: `Packer.PackManifestAsync()` for packing, `ReadOnlyTargetExtensions.CopyAsync()` for copy, `IRepository.FetchReferrersAsync()` for discover. Auth via `Registry.Remote.Auth.Client` + `ICredentialProvider`.

**Critical gaps in oras-dotnet**:
- No credential *store* (only credential *provider*) — must build Docker config.json store
- No OCI layout store — defer `--oci-layout` support
- No manifest index helpers — must manually construct `Oci.Index`
- Library marked "under initial development" — APIs may break

**Architecture decisions (9 ADRs)**:
- System.CommandLine for CLI parsing (ADR-001)
- Service layer between commands and library (ADR-002)
- Docker-compatible credential store (ADR-003)
- IOutputFormatter abstraction with text/JSON (ADR-004)
- Defer OCI layout + experimental commands (ADR-005)
- .NET 10 + AOT ready (ADR-006)
- Central package management (ADR-007)
- Structured user errors with recommendations (ADR-008)
- Phase 1 command scope defined (ADR-009)

**Architecture decisions (9 ADRs)**:
- System.CommandLine for CLI parsing (ADR-001)
- Service layer between commands and library (ADR-002)
- Docker-compatible credential store (ADR-003)
- IOutputFormatter abstraction with text/JSON (ADR-004)
- Defer OCI layout + experimental commands (ADR-005)
- .NET 10 + AOT ready (ADR-006)
- Central package management (ADR-007)
- Structured user errors with recommendations (ADR-008)
- Phase 1 command scope defined (ADR-009)

**Key file paths**:
- `docs/design-review.md` — full architecture design review
- `.squad/decisions.md` — 9 ADRs + OrasProject.Oras v0.5.0 decision (merged from inbox)

### 2026-03-06 — Library API Mapping from Dallas

**Go CLI → .NET Library Command Mapping:**
- **10 commands with direct library equivalents**: pull, push, tag, resolve, copy, attach, discover, blob*, manifest*, repo*
- **2 commands need CLI-level implementation**: login/logout (library has ICredentialProvider interface), version (assembly reflection)
- **2 commands with hybrid implementation**: backup/restore (library has CopyAsync for DAG traversal; CLI implements serialization format), manifest index (library has Index class; CLI implements creation/manipulation)

**Critical for Implementation:**
- **Credential Storage Gap (ADR-003):** Library only provides ICredentialProvider interface. Dallas's research + Ripley's ADR-003 decision = CLI must implement Docker config.json credential store with credential helper protocol (oras-credentials-*).
- **Authentication Pattern:** Library's Challenge, Scope, ICache classes available. CLI service layer will extend ICache for persistent token storage.
- **OCI Layout Gap (ADR-005):** No OCI layout store in library. Phase 1 focuses on remote registries only.

**Design Alignment:**
- Service layer (ADR-002) will wrap library operations for testability
- All operations async-first (library pattern)
- Options pattern for command flags (library pattern)
- ITarget interface polymorphism for multi-target operations

### 2026-03-06 — PRD Authored

**PRD structure**: 7 sections — Product Overview, Command Reference (full parity), Interactive TUI, Non-Functional Requirements, Testing Strategy, CI/CD & Release, Work Breakdown. Written to `docs/prd.md`.

**Priority tiers finalized**:
- P0 (11 commands): push, pull, login, logout, version, tag, resolve, copy, repo ls, repo tags, manifest fetch — promoted `copy` and `resolve` to P0 (essential for CI/CD workflows; low implementation risk since library has direct API mapping)
- P1 (8 commands): attach, discover, blob fetch/push/delete, manifest push/delete, manifest fetch-config
- P2 (2 commands): manifest index create/update — manual `Oci.Index` construction, limited use case
- Deferred: backup, restore, `--oci-layout` (unchanged from ADR-005/009)

**Key scope calls**:
- `--format go-template` dropped — no Go template equivalent in .NET; JSON is the machine-readable alternative
- TUI is Sprint 3, not Sprint 1 — non-interactive CLI must be solid first
- Credential store is Sprint 1 critical path — login/logout blocks everything that touches registries
- Integration tests use testcontainers-dotnet from Sprint 1 — not deferred
- AOT configuration deferred to Sprint 4 but designed-for from day one

**Work breakdown**: 4 sprints, 51 work items. Dallas carries the command implementation load. Bishop owns TUI and output formatting. Hicks owns test infrastructure and cross-platform validation. Vasquez owns CI/CD and AOT.

**Critical path**: S1-01 (project structure) → S1-05 (credentials) → S1-06 (login) → S1-09 (push) → S1-13 (integration tests) → S2-* (all commands) → S4-01 (AOT)
