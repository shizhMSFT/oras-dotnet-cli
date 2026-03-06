# Squad Decisions

## Active Decisions

### DEC-PRD-001: Promote `copy` and `resolve` to P0

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** `oras copy` and `oras resolve` are P0 (must-have for MVP), not P1.

**Rationale:** Both are essential for CI/CD pipeline workflows (copy between staging→production registries, resolve tags to digests for pinning). Both have direct library API mappings with zero implementation gaps. Low risk, high value.

---

### DEC-PRD-002: Drop `--format go-template` Support

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** The .NET CLI will not support `--format go-template`. Only `text` and `json` formats are supported.

**Rationale:** Go templates have no direct .NET equivalent. JSON output covers the machine-readable use case. A template engine (Scriban/Liquid) can be evaluated later if users request it. Shipping without it avoids scope creep and an awkward API mismatch.

---

### DEC-PRD-003: TUI is Sprint 3 — Non-Interactive First

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Interactive TUI mode is Sprint 3 work. Sprints 1–2 focus entirely on non-interactive CLI command parity.

**Rationale:** The TUI builds on top of the command/service layer. If we try to build both simultaneously, we'll compromise the command layer's API surface. CI/CD users (the largest audience) need non-interactive mode first. TUI is a differentiator, not a blocker.

---

### DEC-PRD-004: Integration Tests from Sprint 1

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Integration tests (testcontainers-dotnet + OCI registry) are set up in Sprint 1, not deferred.

**Rationale:** Push/pull/login are network-dependent operations that can't be fully validated with mocks alone. Catching integration issues early avoids costly rework. The test infrastructure investment pays for itself immediately.

---

### DEC-PRD-005: 4-Sprint / 8-Week Timeline

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Work is decomposed into 4 two-week sprints: Foundation → Parity → TUI → Release.

**Rationale:** Two-week sprints provide clear milestones and integration points. Sprint 1 produces a usable (if minimal) CLI. Sprint 2 reaches Go CLI parity. Sprint 3 adds the TUI differentiator. Sprint 4 hardens for release.

---

### DEC-PRD-006: Exit Code Convention — Match Go CLI

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Exit codes follow Go CLI: 0 = success, 1 = general error, 2 = argument error.

**Rationale:** Users switching from Go CLI should not need to update their scripts' exit code handling. This is a compatibility requirement, not just a convention.

---

### User Directive: System.CommandLine 2.x Reference

**Date:** 2026-03-06  
**Source:** Shiwei Zhang (via Copilot)  
**Status:** Active

**Directive:** For anything related to System.CommandLine, refer to the official Microsoft documentation: [Overview](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) and [Syntax](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax). The latest System.CommandLine is 2.x, which is newer than the version in LLM training data. Always fetch these docs before writing System.CommandLine code.

**Context:** User request — captured for team memory.

---

### ADR-001: System.CommandLine as CLI Framework

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Use `System.CommandLine` for command parsing and CLI structure.

**Rationale:** First-party Microsoft library with 1:1 mapping to Go cobra command tree. Built-in help generation, tab completion support, response files. Spectre.Console is for rendering only — not CLI parsing.

**Alternatives considered:** Spectre.Console.Cli (mixing rendering and parsing creates coupling), CliFx (smaller community).

---

### ADR-002: Service Layer Between Commands and Library

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Commands never call `OrasProject.Oras` library directly. A thin service layer sits between.

**Rationale:** Testability (mock services in unit tests without registry), centralized progress reporting, clean error translation boundary. Services orchestrate — they don't duplicate library logic.

---

### ADR-003: Docker-Compatible Credential Store

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Implement Docker `config.json` credential store with credential helper protocol.

**Rationale:** Cross-compatibility with Go CLI is non-negotiable. Users must not re-login when switching between CLI implementations. The Go CLI uses `oras-credentials-go` which reads `~/.docker/config.json` and shells out to `docker-credential-*` helpers.

---

### ADR-004: Output Formatting via IOutputFormatter

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Abstract output through `IOutputFormatter` with `TextFormatter` (Spectre.Console) and `JsonFormatter` implementations. Defer template support.

**Rationale:** Go CLI supports `--format text|json|go-template`. .NET will ship with text+JSON first. Go templates have no .NET equivalent; Scriban/Liquid can be evaluated later.

---

### ADR-005: Defer OCI Layout and Experimental Commands

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Phase 1 targets remote registries only. `--oci-layout`, `backup`, and `restore` are deferred.

**Rationale:** oras-dotnet has no OCI layout store. Building one is significant scope. Ship core value (remote registry operations) first.

---

### ADR-006: .NET 10 + Native AOT Ready

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Target `net10.0` exclusively. Design for AOT from day one.

**Rationale:** Owner specified .NET 10. AOT enables small, fast single-file binaries competitive with Go. Avoid reflection; use source generators for JSON.

---

### ADR-007: Central Package Management

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Use `Directory.Packages.props` for all NuGet version pinning.

**Rationale:** Multi-project solution (CLI + tests) needs version consistency. Standard .NET practice.

---

### ADR-008: Error Handling — Structured User Errors

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Define an `OrasCliException` base with `Message` and `Recommendation` fields. Catch library exceptions in the service layer and translate. Exit codes: 0 success, 1 error, 2 argument error.

**Rationale:** Matches Go CLI's `Error:` / `Recommendation:` output pattern. Provides actionable guidance to users.

---

### ADR-009: Phase 1 Command Scope

**Date:** 2026-03-06  
**Author:** Ripley (Lead/Architect)  
**Status:** Proposed

**Decision:** Phase 1 ships: push, pull, copy, tag, resolve, attach, discover, blob (fetch/push/delete), manifest (fetch/push/delete), repo (ls/tags), login, logout, version. Defers: manifest index, backup, restore, OCI layout.

**Rationale:** Covers the complete remote registry workflow. Deferred items require missing library support or are experimental in Go CLI.

---

### Decision: OrasProject.Oras Package Version

**Date:** 2026-03-06  
**Author:** Vasquez (DevOps)  
**Status:** For Review

**Context:** During initial project setup, discovered that the OrasProject.Oras NuGet package version 1.2.0 (as originally specified in requirements) does not exist. The latest available version on nuget.org is 0.5.0.

**Decision:** Configured the project to use OrasProject.Oras version 0.5.0 (latest available) instead of 1.2.0.

**Impact:**
- **Immediate:** Project builds and tests successfully with v0.5.0
- **Future:** Team should verify that v0.5.0 contains all required functionality for the CLI
- **API Compatibility:** May need to adjust implementation based on actual v0.5.0 API surface

**Recommendation:** Team should evaluate whether v0.5.0 meets all requirements or if we need to:
1. Wait for a newer version of OrasProject.Oras
2. Contribute missing features to the oras-dotnet library
3. Adjust project requirements based on current library capabilities

**References:**
- NuGet package: https://www.nuget.org/packages/OrasProject.Oras/
- Latest version found: 0.5.0
- Originally specified: 1.2.0 (not found)

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
