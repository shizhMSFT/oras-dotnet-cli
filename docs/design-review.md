# ORAS .NET CLI — Architecture Design Review

**Author:** Ripley (Lead/Architect)
**Date:** 2026-03-06
**Status:** Draft — feeds into PRD

---

## 1. Go CLI Command Surface Analysis

### 1.1 Complete Command Tree

The Go `oras` CLI (https://github.com/oras-project/oras) exposes the following command surface via `cobra`:

```
oras
├── push          Push files to a registry or OCI layout
├── pull          Pull files from a registry or OCI layout
├── attach        Attach files to an existing artifact
├── discover      Discover referrers of a manifest (via Referrers API)
├── copy (cp)     Copy artifacts between registries/layouts
├── tag           Tag a manifest in a registry
├── resolve       Resolve a tag to a digest
├── login         Log in to a remote registry
├── logout        Log out from a remote registry
├── version       Show version information
├── backup        [Experimental] Backup OCI artifacts
├── restore       [Experimental] Restore OCI artifacts
├── blob
│   ├── fetch     Get a blob from a registry
│   ├── push      Push a blob to a registry
│   └── delete    Delete a blob from a registry
├── manifest
│   ├── fetch         Fetch manifest from a registry
│   ├── fetch-config  Fetch config of a manifest
│   ├── push          Push a manifest to a registry
│   ├── delete        Delete a manifest from a registry
│   └── index
│       ├── create    Create an OCI image index
│       └── update    Update an OCI image index
└── repo
    ├── ls        List repositories in a registry
    └── tags      List tags in a repository
```

### 1.2 Common Flag Patterns (Go CLI)

The Go CLI uses composable option structs:

| Option Group | Flags | Used By |
|---|---|---|
| `Common` | `--debug`, `--no-tty` | All commands |
| `Remote` | `--username/-u`, `--password/-p`, `--password-stdin`, `--identity-token`, `--identity-token-stdin`, `--insecure`, `--plain-http`, `--ca-file`, `--cert-file`, `--key-file`, `--registry-config` | All registry commands |
| `Target` | `<name>:<ref>`, `--oci-layout`, `--oci-layout-path` | push, pull, attach, cp, tag, resolve, discover |
| `Packer` | `--annotation`, `--annotation-file`, `--export-manifest` | push, attach |
| `ImageSpec` | `--image-spec` (`v1.0` / `v1.1`) | push, attach |
| `Format` | `--format` (`text`, `json`, `go-template`) | push, pull, attach, cp, discover, manifest fetch, etc. |
| `Platform` | `--platform` | pull, cp, manifest fetch, resolve |
| `Terminal` | TTY detection | push, pull, attach, cp |

### 1.3 Output Formats

The Go CLI supports three output modes:
- **Text** (default): Human-readable status lines
- **JSON**: Machine-parseable JSON output
- **Go template**: Custom Go template formatting

### 1.4 Key Behavioral Patterns

- **TTY detection**: Progress bars when terminal detected; plain text otherwise
- **Concurrency flag**: `--concurrency` on push/cp (default 5)
- **Dual target**: Commands accept either registry references or `--oci-layout` for local OCI layout directories
- **Credential flow**: Interactive prompts for login; Docker config.json credential store integration

---

## 2. oras-dotnet Library API Analysis

### 2.1 Core API Surface

The `OrasProject.Oras` NuGet package (https://github.com/oras-project/oras-dotnet) provides:

#### High-Level Operations (Extension Methods)

| Method | Namespace | Maps To |
|---|---|---|
| `ReadOnlyTargetExtensions.CopyAsync()` | `OrasProject.Oras` | `oras copy`, `oras push` (local→remote) |
| `Packer.PackManifestAsync()` | `OrasProject.Oras` | `oras push` (manifest packing) |

#### Core Interfaces

```
IReadOnlyTarget (IReadOnlyStorage + IResolvable)
  └── ITarget (IStorage + ITagStore + IReadOnlyTarget)

IReadOnlyStorage
  ├── ExistsAsync(Descriptor)
  └── FetchAsync(Descriptor)

IStorage : IReadOnlyStorage
  └── PushAsync(Descriptor, Stream)

IResolvable
  └── ResolveAsync(string reference)

ITagStore
  └── TagAsync(Descriptor, string reference)

IDeletable
  └── DeleteAsync(Descriptor)

IFetchable / IPushable / ITaggable
IPredecessorFindable
IReadOnlyGraphStorage
```

#### Registry Layer

```
IRegistry
  ├── GetRepositoryAsync(name)
  └── ListRepositoriesAsync(last?)

IRepository : ITarget + IBlobLocationProvider + IReferenceFetchable
            + IReferencePushable + IDeletable + ITagListable + IMounter
  ├── Blobs → IBlobStore
  ├── Manifests → IManifestStore
  └── FetchReferrersAsync(descriptor, artifactType?)

Registry.Remote.Registry    — IRegistry implementation for remote
Registry.Remote.Repository  — IRepository implementation for remote
Registry.Remote.BlobStore   — IBlobStore implementation
Registry.Remote.ManifestStore — IManifestStore implementation
```

#### Auth Layer

```
Registry.Remote.Auth
  ├── Client            — HTTP client with auth challenge handling
  ├── Credential        — Username/password/token record
  ├── ICredentialProvider — Async credential resolution
  ├── SingleRegistryCredentialProvider
  ├── ICache / Cache    — Token caching
  ├── Scope / ScopeManager
  └── Challenge         — WWW-Authenticate parsing
```

#### OCI Types

```
Oci.Descriptor, Oci.Manifest, Oci.Index, Oci.Platform
Oci.MediaType (constants), Oci.Versioned
Content.Digest, Content.MemoryStore, Content.MemoryStorage
```

#### Exceptions

```
AlreadyExistsException, NotFoundException
InvalidDateTimeFormatException, InvalidMediaTypeException
MissingArtifactTypeException, SizeLimitExceededException
```

### 2.2 Go CLI → .NET Library Mapping

| Go CLI Command | .NET Library API | Gap? |
|---|---|---|
| `oras push` | `Packer.PackManifestAsync()` + `CopyAsync()` | Need file→descriptor helper |
| `oras pull` | `IRepository.FetchAsync()` + `IResolvable.ResolveAsync()` | Need descriptor→file extraction |
| `oras attach` | `Packer.PackManifestAsync()` (with Subject) | ✅ Supported (v1.1) |
| `oras discover` | `IRepository.FetchReferrersAsync()` | ✅ Supported |
| `oras copy` | `ReadOnlyTargetExtensions.CopyAsync()` | ✅ Supported |
| `oras tag` | `ITagStore.TagAsync()` | ✅ Supported |
| `oras resolve` | `IResolvable.ResolveAsync()` | ✅ Supported |
| `oras login` | `Auth.Client` + `ICredentialProvider` | ⚠️ No credential *store* — must build |
| `oras logout` | — | ❌ No credential store management |
| `oras blob fetch` | `IBlobStore.FetchAsync()` | ✅ Supported |
| `oras blob push` | `IBlobStore.PushAsync()` | ✅ Supported |
| `oras blob delete` | `IDeletable.DeleteAsync()` | ✅ Supported |
| `oras manifest fetch` | `IManifestStore.FetchAsync()` / `IReferenceFetchable` | ✅ Supported |
| `oras manifest push` | `IManifestStore.PushAsync()` / `IReferencePushable` | ✅ Supported |
| `oras manifest delete` | `IDeletable.DeleteAsync()` | ✅ Supported |
| `oras manifest index create/update` | Manual OCI Index construction + push | ⚠️ No helper — manual build |
| `oras repo ls` | `IRegistry.ListRepositoriesAsync()` | ✅ Supported |
| `oras repo tags` | `ITagListable.ListTagsAsync()` | ✅ Supported |
| `oras backup` | — | ❌ Experimental, no .NET equivalent |
| `oras restore` | — | ❌ Experimental, no .NET equivalent |

### 2.3 Critical Gap: Credential Store

The Go CLI uses `oras-credentials-go` for Docker-compatible credential store integration (`~/.docker/config.json`, platform-native credential helpers like `docker-credential-desktop`). The .NET library provides `Auth.Client` and `ICredentialProvider` but **does not** include a persistent credential store. This must be implemented in the CLI project.

### 2.4 Critical Gap: OCI Layout

The Go CLI supports `--oci-layout` for local OCI image layout directories. The .NET library does not expose an `OciLayout` store equivalent. This either requires building one or deferring OCI layout support.

---

## 3. Architecture Proposal

### 3.1 Solution Layout

```
oras-dotnet-cli/
├── src/
│   ├── Oras.Cli/                    # Main CLI application (dotnet tool)
│   │   ├── Oras.Cli.csproj
│   │   ├── Program.cs               # Entry point
│   │   ├── Commands/                 # System.CommandLine command definitions
│   │   │   ├── RootCommand.cs
│   │   │   ├── PushCommand.cs
│   │   │   ├── PullCommand.cs
│   │   │   ├── AttachCommand.cs
│   │   │   ├── DiscoverCommand.cs
│   │   │   ├── CopyCommand.cs
│   │   │   ├── TagCommand.cs
│   │   │   ├── ResolveCommand.cs
│   │   │   ├── LoginCommand.cs
│   │   │   ├── LogoutCommand.cs
│   │   │   ├── VersionCommand.cs
│   │   │   ├── Blob/
│   │   │   │   ├── BlobCommand.cs    # Parent "blob" command
│   │   │   │   ├── FetchCommand.cs
│   │   │   │   ├── PushCommand.cs
│   │   │   │   └── DeleteCommand.cs
│   │   │   ├── Manifest/
│   │   │   │   ├── ManifestCommand.cs
│   │   │   │   ├── FetchCommand.cs
│   │   │   │   ├── FetchConfigCommand.cs
│   │   │   │   ├── PushCommand.cs
│   │   │   │   ├── DeleteCommand.cs
│   │   │   │   └── Index/
│   │   │   │       ├── IndexCommand.cs
│   │   │   │       ├── CreateCommand.cs
│   │   │   │       └── UpdateCommand.cs
│   │   │   └── Repo/
│   │   │       ├── RepoCommand.cs
│   │   │       ├── ListCommand.cs
│   │   │       └── TagsCommand.cs
│   │   ├── Options/                  # Shared option types (mirrors Go option pkg)
│   │   │   ├── CommonOptions.cs
│   │   │   ├── RemoteOptions.cs
│   │   │   ├── TargetOptions.cs
│   │   │   ├── PackerOptions.cs
│   │   │   ├── FormatOptions.cs
│   │   │   └── PlatformOptions.cs
│   │   ├── Services/                 # Orchestration layer over oras-dotnet
│   │   │   ├── RegistryService.cs
│   │   │   ├── PushService.cs
│   │   │   ├── PullService.cs
│   │   │   ├── CopyService.cs
│   │   │   └── CredentialService.cs
│   │   ├── Credentials/             # Credential store implementation
│   │   │   ├── DockerConfigStore.cs
│   │   │   ├── NativeCredentialHelper.cs
│   │   │   └── CredentialProviderFactory.cs
│   │   ├── Output/                   # Display/rendering layer
│   │   │   ├── IOutputFormatter.cs
│   │   │   ├── TextFormatter.cs
│   │   │   ├── JsonFormatter.cs
│   │   │   └── ProgressRenderer.cs   # Spectre.Console progress
│   │   └── FileRef/                  # File reference parsing
│   │       └── FileReference.cs
│   └── Oras.Cli.Abstractions/       # (Optional) Shared interfaces
│       └── Oras.Cli.Abstractions.csproj
├── test/
│   ├── Oras.Cli.Tests/              # Unit tests (xUnit)
│   │   ├── Commands/
│   │   ├── Services/
│   │   ├── Credentials/
│   │   └── Output/
│   └── Oras.Cli.IntegrationTests/   # Integration tests (testcontainers)
│       └── RegistryTests.cs
├── docs/
│   └── design-review.md             # This document
├── oras-dotnet-cli.sln
├── Directory.Build.props             # Shared build properties
├── Directory.Packages.props          # Central package management
├── global.json                       # .NET SDK version pinning
└── .editorconfig
```

### 3.2 System.CommandLine Mapping

System.CommandLine maps naturally to the Go CLI's `cobra` tree:

```csharp
// Program.cs
var root = new RootCommand("ORAS - OCI Registry As Storage");

root.AddCommand(new PushCommand());
root.AddCommand(new PullCommand());
root.AddCommand(new AttachCommand());
root.AddCommand(new DiscoverCommand());
root.AddCommand(new CopyCommand());
root.AddCommand(new TagCommand());
root.AddCommand(new ResolveCommand());
root.AddCommand(new LoginCommand());
root.AddCommand(new LogoutCommand());
root.AddCommand(new VersionCommand());

// Subcommand groups
var blobCmd = new Command("blob", "Blob operations");
blobCmd.AddCommand(new BlobFetchCommand());
blobCmd.AddCommand(new BlobPushCommand());
blobCmd.AddCommand(new BlobDeleteCommand());
root.AddCommand(blobCmd);

var manifestCmd = new Command("manifest", "Manifest operations");
manifestCmd.AddCommand(new ManifestFetchCommand());
manifestCmd.AddCommand(new ManifestFetchConfigCommand());
manifestCmd.AddCommand(new ManifestPushCommand());
manifestCmd.AddCommand(new ManifestDeleteCommand());

var indexCmd = new Command("index", "Index operations");
indexCmd.AddCommand(new ManifestIndexCreateCommand());
indexCmd.AddCommand(new ManifestIndexUpdateCommand());
manifestCmd.AddCommand(indexCmd);
root.AddCommand(manifestCmd);

var repoCmd = new Command("repo", "Repository operations");
repoCmd.AddCommand(new RepoListCommand());
repoCmd.AddCommand(new RepoTagsCommand());
root.AddCommand(repoCmd);

return await root.InvokeAsync(args);
```

**Shared options** use System.CommandLine's `Option<T>` and are composed into commands via helper methods — mirroring Go's composable option structs:

```csharp
public static class RemoteOptions
{
    public static Option<string?> Username { get; } = new("--username", "-u");
    public static Option<string?> Password { get; } = new("--password", "-p");
    public static Option<bool> PasswordStdin { get; } = new("--password-stdin");
    public static Option<bool> Insecure { get; } = new("--insecure");
    public static Option<bool> PlainHttp { get; } = new("--plain-http");
    // ...

    public static void ApplyTo(Command command)
    {
        command.AddOption(Username);
        command.AddOption(Password);
        // ...
    }
}
```

### 3.3 Spectre.Console Integration

Spectre.Console handles two concerns:

1. **Progress rendering**: `AnsiConsole.Progress()` for push/pull/copy transfer progress — replaces Go's TTY-aware status tracking
2. **Rich output**: Tables for `repo ls`, `repo tags`, `discover` output; tree rendering for referrers

Integration approach:
- **`IOutputFormatter` interface** abstracts output rendering
- **`TextFormatter`** uses Spectre.Console for TTY-detected terminals (colors, progress bars, tables)
- **`JsonFormatter`** bypasses Spectre and writes raw JSON to stdout
- **`ProgressRenderer`** wraps `AnsiConsole.Progress()` and implements the `CopyGraphOptions.PreCopy`/`PostCopy` callbacks from the library

TTY detection mirrors Go CLI behavior:
```csharp
bool isTty = !Console.IsOutputRedirected && !Console.IsErrorRedirected;
```

### 3.4 Library Consumption: Service Layer

Commands **do not** call oras-dotnet directly. A thin service layer provides:

1. **Testability**: Services are injected and mockable
2. **Cross-cutting concerns**: Logging, error translation, progress reporting
3. **Abstraction over target resolution**: Registry vs. OCI layout

```
Command → Service → oras-dotnet library
   ↓         ↓
Options   IOutputFormatter
```

Example flow for `oras push`:
```
PushCommand
  → validates args, binds options
  → calls PushService.PushAsync(...)
    → creates file store, packs layers
    → calls Packer.PackManifestAsync(...)
    → calls ReadOnlyTargetExtensions.CopyAsync(...)
    → reports progress via ProgressRenderer
  → OutputFormatter renders result
```

### 3.5 Error Handling Strategy

| Layer | Strategy |
|---|---|
| **Library exceptions** | Catch oras-dotnet exceptions (`NotFoundException`, `AlreadyExistsException`, etc.) in the service layer; translate to user-friendly CLI errors |
| **CLI argument errors** | System.CommandLine handles validation; custom validators for reference format, file existence |
| **Network errors** | Catch `HttpRequestException`, `TaskCanceledException`; provide actionable messages ("check registry URL", "check credentials") |
| **Exit codes** | 0 = success, 1 = general error, 2 = argument error (follow Go CLI conventions) |
| **Debug output** | `--debug` flag enables verbose logging to stderr via `ILogger` |

Error format mirrors Go CLI:
```
Error: <message>
Recommendation: <actionable guidance>
```

### 3.6 Credential Management (Login/Logout)

Since oras-dotnet lacks a credential store, the CLI must implement one:

1. **Primary**: Read/write Docker's `~/.docker/config.json` — exact same format as Go CLI
2. **Credential helpers**: Shell out to `docker-credential-*` helpers (desktop, pass, secretservice, wincred) — same protocol as Docker/Go CLI
3. **Fallback**: Base64 `auth` field in config.json (for environments without helpers)
4. **`ICredentialProvider` bridge**: Adapt the credential store into oras-dotnet's `ICredentialProvider` interface for the `Auth.Client`

This ensures cross-compatibility: credentials stored by Go `oras login` work with .NET `oras login` and vice versa.

### 3.7 Cross-Platform Considerations

| Concern | Approach |
|---|---|
| **Distribution** | .NET tool (`dotnet tool install -g oras`) + self-contained single-file publish for standalone binaries |
| **Config paths** | `DOCKER_CONFIG` env var → `~/.docker/config.json` (cross-platform) |
| **Credential helpers** | Platform-specific: `docker-credential-wincred` (Windows), `docker-credential-osxkeychain` (macOS), `docker-credential-secretservice` (Linux) |
| **Path separators** | Use `Path.Combine()`, `Path.DirectorySeparatorChar` throughout |
| **TTY detection** | `Console.IsOutputRedirected` works cross-platform on .NET |
| **Line endings** | Normalize to LF in OCI content; use `Environment.NewLine` for CLI output |
| **Native AOT** | Design for AOT compatibility from day one — avoid reflection where possible, use source generators for JSON serialization |

---

## 4. Key Design Decisions

### ADR-001: System.CommandLine as CLI Framework
**Decision**: Use `System.CommandLine` (stable release targeting .NET 10) for command parsing.
**Rationale**: First-party Microsoft library; natural tree structure maps 1:1 to Go's cobra; built-in help generation, tab completion, response files. Spectre.Console for rendering only — not for CLI parsing.

### ADR-002: Service Layer Between Commands and Library
**Decision**: Commands call service classes, not oras-dotnet directly.
**Rationale**: Enables unit testing without registry access; centralizes progress reporting; provides clean error translation boundary. Services are thin — they orchestrate, not duplicate library logic.

### ADR-003: Docker-Compatible Credential Store
**Decision**: Implement Docker config.json credential store with credential helper protocol support.
**Rationale**: Cross-compatibility with Go CLI is non-negotiable. Users must not re-login when switching between CLI implementations. The Go CLI uses `oras-credentials-go` which reads `~/.docker/config.json`.

### ADR-004: Output Formatting via IOutputFormatter
**Decision**: Abstract output through `IOutputFormatter` with `TextFormatter` (Spectre.Console), `JsonFormatter`, and potentially `TemplateFormatter` implementations.
**Rationale**: Go CLI supports `--format text|json|go-template`. .NET must match. C# doesn't have Go templates, so we'll initially support text+JSON, with potential Liquid/Scriban template support later.

### ADR-005: Defer OCI Layout and Experimental Commands
**Decision**: Phase 1 targets remote registry operations only. OCI layout (`--oci-layout`) and experimental commands (`backup`, `restore`) are deferred.
**Rationale**: oras-dotnet lacks OCI layout store. Building one is significant scope. Remote registry is the primary use case. Ship core value first.

### ADR-006: .NET 10 + Native AOT Ready
**Decision**: Target `net10.0` only. Design for AOT compatibility from day one.
**Rationale**: Project owner specified .NET 10. Single-target simplifies testing. AOT enables small, fast single-file binaries competitive with Go's output.

### ADR-007: Central Package Management
**Decision**: Use `Directory.Packages.props` for NuGet version pinning.
**Rationale**: Standard .NET practice for multi-project solutions. Prevents version drift between CLI and test projects.

---

## 5. Risks and Constraints

### 5.1 High Risk

| Risk | Impact | Mitigation |
|---|---|---|
| **oras-dotnet is "under initial development"** — APIs may change | Breaking changes force rework | Pin to specific NuGet version; abstract via service layer; contribute upstream if gaps found |
| **No credential store in oras-dotnet** | Must build from scratch; security-sensitive code | Follow Docker credential helper protocol exactly; reference Go implementation; security review |
| **No OCI layout support in oras-dotnet** | Cannot support `--oci-layout` flag | Defer to Phase 2; may contribute OciLayout store upstream |

### 5.2 Medium Risk

| Risk | Impact | Mitigation |
|---|---|---|
| **System.CommandLine maturity** | API surface may shift before .NET 10 GA | Use latest preview; keep command layer thin |
| **Go template parity gap** | `--format go-template` cannot be replicated in .NET | Support `--format json` as machine-readable alternative; consider Scriban templates |
| **Performance parity with Go CLI** | .NET startup time may be slower without AOT | Use Native AOT publish; benchmark early |
| **manifest index operations** | No helper in oras-dotnet for index create/update | Build manually using `Oci.Index` type + `IPushable.PushAsync` |

### 5.3 Constraints

1. **Must use oras-dotnet library** — not raw HTTP calls to registries
2. **Command surface must match Go CLI** — same command names, same flag names where possible
3. **Cross-platform** — Windows, macOS, Linux
4. **Apache 2.0 license** — matching ORAS project licensing
5. **ORAS project review guidelines** apply to contributions

### 5.4 Open Questions

1. **Tab completion**: Should we ship shell completions (bash, zsh, PowerShell, fish) in Phase 1? System.CommandLine supports this.
2. **`--format go-template` replacement**: Is JSON sufficient, or do we need a template engine? If so, which one?
3. **OCI layout timeline**: When does oras-dotnet plan to add OCI layout support? Can we contribute it?
4. **Telemetry**: Any telemetry/analytics requirements?
5. **Plugin system**: The Go CLI doesn't have plugins, but should the .NET CLI consider extensibility?

---

## 6. Phase 1 Command Scope

Based on risk analysis and library readiness, Phase 1 includes:

| Priority | Commands | Rationale |
|---|---|---|
| **P0 (Must)** | `push`, `pull`, `login`, `logout`, `version` | Core workflow |
| **P0 (Must)** | `copy`, `tag`, `resolve` | Essential operations |
| **P1 (Should)** | `attach`, `discover` | OCI artifact workflow |
| **P1 (Should)** | `blob fetch/push/delete` | Low-level operations |
| **P1 (Should)** | `manifest fetch/push/delete` | Low-level operations |
| **P1 (Should)** | `repo ls`, `repo tags` | Discovery operations |
| **P2 (Could)** | `manifest index create/update` | Advanced workflow |
| **P3 (Defer)** | `backup`, `restore`, `--oci-layout` | Requires missing library support |

---

## 7. Dependency Summary

| Package | Purpose | Version Strategy |
|---|---|---|
| `OrasProject.Oras` | OCI registry client library | Pin to latest stable |
| `System.CommandLine` | CLI parsing and command tree | Latest targeting .NET 10 |
| `Spectre.Console` | TUI rendering (progress, tables) | Latest stable |
| `xUnit` + `xunit.runner` | Unit testing | Latest stable |
| `Testcontainers` | Integration testing with real registries | Latest stable |
| `NSubstitute` or `Moq` | Service mocking in tests | Latest stable |

---

*This design review establishes the architectural foundation for the ORAS .NET CLI. It should be used as input for the PRD and implementation planning.*
