# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

### oras-dotnet Library API Surface (2026-03-06)

**Core Architecture:**
- Interface-driven design: All operations use abstract interfaces (ITarget, IRepository, IBlobStore, IManifestStore, etc.)
- Async-first: Every I/O operation is async with CancellationToken support
- Options pattern: Configuration via dedicated Options classes (PackManifestOptions, RepositoryOptions, CopyOptions)
- NuGet package: `OrasProject.Oras` (net8.0+)

**Key Operation Classes:**
1. **Registry** - Main client for connecting to OCI registries; requires ICredentialProvider
2. **Repository** - Implements IRepository with IReadOnlyTarget & ITarget interfaces
3. **Packer** - Static class that creates & packs OCI manifests (v1.0/v1.1); core to push & attach operations
4. **BlobStore/ManifestStore** - Handle blob and manifest CRUD
5. **ReadOnlyTargetExtensions.CopyAsync()** - DAG-based copy for cross-registry artifact transfers

**Critical Namespace Mappings:**
- `OrasProject.Oras.Registry.Remote` - Registry & Repository clients
- `OrasProject.Oras.Registry.Remote.Auth` - Authentication (ICredentialProvider, Challenge, Scope, ICache)
- `OrasProject.Oras.Content` - Storage interfaces & memory implementations (IFetchable, IPushable, IDeletable)
- `OrasProject.Oras.Oci` - OCI models (Manifest, Descriptor, Index, Platform, MediaType)

**Go CLI → .NET Library Mapping Summary:**
- 10/12 top-level commands have direct library equivalents (pull, push, tag, resolve, copy, attach, discover, blob*, manifest*, repo*)
- 2 commands need CLI-level implementation: login/logout (library has ICredentialProvider interface), version (assembly reflection)
- 2 commands have hybrid implementation: backup/restore (library has CopyAsync for DAG traversal; CLI implements serialization format), manifest index (library has Index class; CLI implements creation/manipulation)

**Authentication Model:**
- No built-in credential storage; Registry constructor takes ICredentialProvider
- OAuth2 flow managed by Challenge & Scope classes
- Token caching via ICache interface (CLI must implement storage backend)
- SingleRegistryCredentialProvider available as simple implementation

**Design Patterns to Follow in CLI:**
1. All operations should be async (match library's async-first design)
2. Use Options pattern for command flags (CopyOptions, PackManifestOptions models)
3. Leverage ITarget interface for polymorphism (operations work across local/remote targets)
4. Cache handling for authentication (extend ICache for persistent storage)
5. DAG traversal via Packer & ReadOnlyTargetExtensions for complex operations (backup, recursive copy)

### 2026-03-06 — Architecture Decisions (Cross-Pollination from Ripley)

**CLI Framework:** System.CommandLine (ADR-001) — aligns with library's interface-driven design for service layer integration.

**Service Layer:** Commands orchestrate through services (ADR-002), enabling clean separation from library calls. This directly supports the authentication pattern you identified: services will handle ICredentialProvider implementation and Docker config.json credential store (ADR-003).

**Output Abstraction:** IOutputFormatter pattern (ADR-004) works well with Spectre.Console rendering already in your library survey.

**Critical for Implementation:**
- Phase 1 scope (ADR-009): 10/12 commands have direct library equivalents per your mapping. Service layer will wrap these.
- Credential Storage (ADR-003): Library only has ICredentialProvider interface; you identified this gap. CLI will implement Docker config.json store + credential helper protocol.
- Auth Model Alignment: Your ICache extension will feed into structured error handling (ADR-008).
