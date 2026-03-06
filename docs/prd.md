# ORAS .NET CLI — Product Requirements Document

**Author:** Ripley (Lead/Architect)
**Date:** 2026-03-06
**Status:** Draft
**Version:** 1.0

---

## Table of Contents

1. [Product Overview](#1-product-overview)
2. [Command Reference](#2-command-reference)
3. [Interactive Mode (TUI) Requirements](#3-interactive-mode-tui-requirements)
4. [Non-Functional Requirements](#4-non-functional-requirements)
5. [Testing Strategy](#5-testing-strategy)
6. [CI/CD and Release](#6-cicd-and-release)
7. [Work Breakdown](#7-work-breakdown)

---

## 1. Product Overview

### 1.1 Vision

The `oras` .NET CLI is a cross-platform .NET 10 reimagining of the [Go oras CLI](https://github.com/oras-project/oras) for managing OCI artifacts in container registries. It is not a port — it is a native .NET experience built on the [oras-dotnet](https://github.com/oras-project/oras-dotnet) library (`OrasProject.Oras`), leveraging the .NET ecosystem's strengths: rich TUI via Spectre.Console, native AOT compilation for fast startup, and first-class async/await patterns.

The CLI achieves **full command parity** with the Go CLI for remote registry operations, while adding an interactive TUI mode that the Go CLI lacks entirely.

### 1.2 Target Users

| Persona | Description | Key Workflows |
|---------|-------------|---------------|
| **Application Developer** | Pushes/pulls artifacts during development | `push`, `pull`, `login`, `tag`, interactive browsing |
| **DevOps Engineer** | Manages artifacts across environments | `copy`, `tag`, `repo ls`, `repo tags`, scripted pipelines |
| **CI/CD Pipeline** | Automated, non-interactive artifact operations | `push`, `pull`, `copy` with `--format json`, exit codes |
| **Platform Engineer** | Operates registries, audits content | `discover`, `manifest fetch`, `blob` operations, `repo ls` |

### 1.3 Core Value Proposition

1. **Full Go CLI parity** — Same command names, same flag names, same exit codes. Users switch without relearning.
2. **Interactive TUI mode** — Registry browser, manifest tree view, live progress — capabilities the Go CLI does not have.
3. **.NET-native distribution** — `dotnet tool install -g oras` for .NET developers; self-contained single-file binaries for everyone else.
4. **Native AOT** — Startup time and binary size competitive with Go.
5. **Cross-credential compatibility** — Shares `~/.docker/config.json` with Go CLI. No re-login required.

### 1.4 Scope Boundaries

**In scope (Phase 1–4):**
- All remote registry commands from the Go CLI
- Interactive TUI mode (new capability)
- Docker-compatible credential store
- Cross-platform binaries (Windows, macOS, Linux)
- `--format text|json` output modes

**Out of scope (deferred):**
- `--oci-layout` support (oras-dotnet lacks OCI layout store)
- `backup` / `restore` commands (experimental in Go CLI; no .NET library support)
- `--format go-template` (no Go template equivalent in .NET; JSON is the machine-readable format)
- Plugin/extensibility system

### 1.5 Architecture Summary

```
User ──► System.CommandLine (CLI parsing)
              │
              ▼
         Command Layer (thin: validates args, binds options)
              │
              ▼
         Service Layer (orchestration, error translation, progress)
              │
              ▼
         OrasProject.Oras Library (registry operations)
              │
              ▼
         OCI-compliant Registry (remote)
```

Key architectural decisions (see `docs/design-review.md` for full ADRs):
- **ADR-001:** System.CommandLine for CLI parsing
- **ADR-002:** Service layer between commands and library
- **ADR-003:** Docker-compatible credential store
- **ADR-004:** IOutputFormatter abstraction (text + JSON)
- **ADR-005:** Defer OCI layout and experimental commands
- **ADR-006:** .NET 10 + Native AOT ready
- **ADR-007:** Central package management
- **ADR-008:** Structured user errors with recommendations
- **ADR-009:** Phase 1 command scope

### 1.6 Technology Stack

| Component | Technology | Version Strategy |
|-----------|------------|------------------|
| Runtime | .NET 10 | `net10.0` TFM only |
| CLI Framework | System.CommandLine | Latest 2.x for .NET 10 |
| TUI Rendering | Spectre.Console | Latest stable |
| OCI Library | OrasProject.Oras | Pin to 0.5.x; abstract via service layer |
| Unit Testing | xUnit | Latest stable |
| Integration Testing | testcontainers-dotnet | Latest stable |
| Mocking | NSubstitute or Moq | Latest stable |
| Package Management | Central (Directory.Packages.props) | — |

---

## 2. Command Reference

### 2.1 Priority Tiers

| Tier | Definition | Commands |
|------|-----------|----------|
| **P0** | Must-have for MVP. Core push/pull workflow. | `push`, `pull`, `login`, `logout`, `version`, `tag`, `repo ls`, `repo tags`, `manifest fetch`, `resolve`, `copy` |
| **P1** | Important for full workflow coverage. | `attach`, `discover`, `blob fetch`, `blob push`, `blob delete`, `manifest push`, `manifest delete`, `manifest fetch-config` |
| **P2** | Nice-to-have. Advanced/niche operations. | `manifest index create`, `manifest index update` |

### 2.2 Global Options

These options apply to all commands:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--debug` | `bool` | `false` | Enable verbose debug logging to stderr |
| `--no-tty` | `bool` | `false` | Force non-interactive mode (no progress bars, no prompts) |
| `--format` | `string` | `text` | Output format: `text` or `json` |
| `--help` / `-h` | — | — | Show help for the command |

### 2.3 Shared Option Groups

#### Remote Options
Used by all commands that interact with a registry.

| Option | Type | Description |
|--------|------|-------------|
| `--username` / `-u` | `string?` | Registry username |
| `--password` / `-p` | `string?` | Registry password |
| `--password-stdin` | `bool` | Read password from stdin |
| `--identity-token` | `string?` | Identity token for authentication |
| `--identity-token-stdin` | `bool` | Read identity token from stdin |
| `--insecure` | `bool` | Allow insecure connections (skip TLS verification) |
| `--plain-http` | `bool` | Use plain HTTP instead of HTTPS |
| `--ca-file` | `string?` | Path to CA certificate file |
| `--registry-config` | `string?` | Path to registry config file (default: `~/.docker/config.json`) |

#### Target Options
Used by commands that accept an artifact reference.

| Option | Type | Description |
|--------|------|-------------|
| `<reference>` | `string` (argument) | Registry reference in `<registry>/<repository>[:<tag>|@<digest>]` format |

#### Packer Options
Used by `push` and `attach`.

| Option | Type | Description |
|--------|------|-------------|
| `--annotation` | `string[]` | Add annotations (`key=value` pairs) |
| `--annotation-file` | `string?` | Path to JSON annotation file |
| `--export-manifest` | `string?` | Export the packed manifest to a file |
| `--image-spec` | `string` | OCI image spec version: `v1.0` or `v1.1` (default: `v1.1`) |

#### Platform Options
Used by `pull`, `copy`, `manifest fetch`, `resolve`.

| Option | Type | Description |
|--------|------|-------------|
| `--platform` | `string?` | Platform in `os/arch[/variant]` format |

---

### 2.4 P0 Commands (Must-Have for MVP)

#### `oras push`

Push files to a remote registry.

```
oras push [options] <reference> [file[:type] ...]
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — target reference (required); `[file[:type] ...]` — files to push with optional media type |
| **Options** | Remote, Packer, `--artifact-type <type>`, `--concurrency <n>` (default: 5), `--disable-path-validation` |
| **Non-interactive** | Outputs each layer pushed with digest and size, then the manifest descriptor. Exit code 0 on success. `--format json` outputs structured JSON. |
| **Interactive (TUI)** | Live progress bar per layer (Spectre.Console), overall transfer progress, transfer speed display. |
| **Exit codes** | `0` success, `1` error (auth, network, server), `2` argument error |
| **Library API** | `Packer.PackManifestAsync()` → `ReadOnlyTargetExtensions.CopyAsync()` |
| **Priority** | P0 |

**Example output (text):**
```
Uploading 3a1bc987ef01 hello.txt
Uploaded  3a1bc987ef01 hello.txt
Pushed [registry] localhost:5000/hello:latest
Digest: sha256:abc123...
```

**Example output (JSON):**
```json
{
  "reference": "localhost:5000/hello:latest",
  "mediaType": "application/vnd.oci.image.manifest.v1+json",
  "digest": "sha256:abc123...",
  "size": 512
}
```

---

#### `oras pull`

Pull files from a remote registry.

```
oras pull [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — source reference (required) |
| **Options** | Remote, Platform, `--output <dir>` (default: current directory), `--keep-old-files`, `--concurrency <n>` |
| **Non-interactive** | Lists each layer downloaded with digest and filename. Exit code 0 on success. |
| **Interactive (TUI)** | Live progress bar per layer download, overall progress. |
| **Exit codes** | `0` success, `1` error, `2` argument error |
| **Library API** | `IReferenceFetchable.ResolveAsync()` → `IBlobStore.FetchAsync()` per layer |
| **Priority** | P0 |

**Example output (text):**
```
Downloading 3a1bc987ef01 hello.txt
Downloaded  3a1bc987ef01 hello.txt
Pulled [registry] localhost:5000/hello:latest
Digest: sha256:abc123...
```

---

#### `oras login`

Authenticate with a remote registry.

```
oras login [options] <registry>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<registry>` — registry hostname (required) |
| **Options** | `--username/-u`, `--password/-p`, `--password-stdin`, `--identity-token`, `--identity-token-stdin`, `--insecure`, `--plain-http`, `--registry-config` |
| **Non-interactive** | Requires `--username` and `--password` (or `--password-stdin`). Validates credentials against registry. Stores in Docker config.json. Outputs "Login Succeeded" on success. |
| **Interactive (TUI)** | Prompts for username (if not provided), prompts for password with masked input (if not provided). |
| **Exit codes** | `0` success, `1` auth failure or network error, `2` argument error |
| **Credential Storage** | Writes to `~/.docker/config.json` (or `$DOCKER_CONFIG/config.json`). Uses platform credential helpers (`docker-credential-wincred`, `docker-credential-osxkeychain`, `docker-credential-secretservice`) when configured. Falls back to base64 `auth` field. |
| **Library API** | CLI-level implementation. Uses `Auth.Client` to validate, then stores credentials. |
| **Priority** | P0 |

**Example output:**
```
Login Succeeded
```

---

#### `oras logout`

Remove stored credentials for a registry.

```
oras logout <registry>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<registry>` — registry hostname (required) |
| **Options** | `--registry-config` |
| **Non-interactive** | Removes credential entry from Docker config.json. Always succeeds silently (exit 0) even if no credential existed. |
| **Interactive (TUI)** | Same as non-interactive (no prompts needed). |
| **Exit codes** | `0` success |
| **Library API** | CLI-level implementation (credential store management). |
| **Priority** | P0 |

---

#### `oras version`

Display version information.

```
oras version
```

| Aspect | Detail |
|--------|--------|
| **Non-interactive** | Outputs version string including CLI version, library version, Go CLI parity version, runtime, and commit hash. |
| **Interactive (TUI)** | Same as non-interactive. |
| **Exit codes** | `0` always |
| **Library API** | Assembly version reflection. |
| **Priority** | P0 |

**Example output:**
```
oras version 0.1.0
OrasProject.Oras: 0.5.0
Runtime: .NET 10.0.0
Platform: win-x64
Commit: abc1234
```

---

#### `oras tag`

Create a tag for an existing manifest.

```
oras tag [options] <source-reference> <target-tag> [<target-tag> ...]
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<source-reference>` — manifest to tag (required); `<target-tag>` — one or more new tags (required) |
| **Options** | Remote |
| **Non-interactive** | Outputs each tag created. Exit code 0 on success. |
| **Interactive (TUI)** | Same as non-interactive. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `ITagStore.TagAsync()` per target tag |
| **Priority** | P0 |

**Example output:**
```
Tagged localhost:5000/hello@sha256:abc123 as latest
Tagged localhost:5000/hello@sha256:abc123 as v1.0
```

---

#### `oras resolve`

Resolve a tag to a digest.

```
oras resolve [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — reference to resolve (required) |
| **Options** | Remote, Platform |
| **Non-interactive** | Outputs the resolved digest. Exit code 0 on success. |
| **Interactive (TUI)** | Same as non-interactive. |
| **Exit codes** | `0` success, `1` not found or error |
| **Library API** | `IReferenceFetchable.ResolveAsync()` → `Descriptor.Digest` |
| **Priority** | P0 |

**Example output:**
```
sha256:abc123def456...
```

---

#### `oras copy`

Copy artifacts between registries or within the same registry.

```
oras copy [options] <source-reference> <destination-reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<source-reference>` (required), `<destination-reference>` (required) |
| **Options** | Remote (for both src and dst), Platform, `--recursive` / `-r`, `--concurrency <n>` |
| **Non-interactive** | Outputs each blob/manifest copied. Exit code 0 on success. `--format json` outputs structured result. |
| **Interactive (TUI)** | Live progress bars for source fetch and destination push. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `ReadOnlyTargetExtensions.CopyAsync()` with `CopyOptions` |
| **Priority** | P0 |

**Example output (text):**
```
Copying 3a1bc987ef01 hello.txt
Copied  3a1bc987ef01 hello.txt
Copied [registry] localhost:5000/hello:latest => localhost:5001/hello:latest
Digest: sha256:abc123...
```

---

#### `oras repo ls`

List repositories in a registry.

```
oras repo ls [options] <registry>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<registry>` — registry hostname (required) |
| **Options** | Remote, `--last <name>` (pagination marker) |
| **Non-interactive** | Outputs one repository name per line. |
| **Interactive (TUI)** | Rendered as a selectable table with Spectre.Console. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `IRegistry.ListRepositoriesAsync()` |
| **Priority** | P0 |

---

#### `oras repo tags`

List tags in a repository.

```
oras repo tags [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — repository reference (required) |
| **Options** | Remote, `--last <tag>` (pagination marker) |
| **Non-interactive** | Outputs one tag per line. |
| **Interactive (TUI)** | Rendered as a selectable table. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `ITagListable.TagsAsync()` |
| **Priority** | P0 |

---

#### `oras manifest fetch`

Fetch a manifest from a registry.

```
oras manifest fetch [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — manifest reference (required) |
| **Options** | Remote, Platform, `--descriptor` (output descriptor only, not full manifest), `--output <file>` (write to file), `--pretty` (pretty-print JSON) |
| **Non-interactive** | Outputs the manifest JSON (or descriptor JSON if `--descriptor`). |
| **Interactive (TUI)** | Syntax-highlighted JSON manifest with tree view of layers. |
| **Exit codes** | `0` success, `1` not found or error |
| **Library API** | `IManifestStore.FetchAsync()` / `IReferenceFetchable.FetchAsync()` |
| **Priority** | P0 |

---

### 2.5 P1 Commands (Important)

#### `oras attach`

Attach files as a referrer artifact to an existing manifest.

```
oras attach [options] <reference> [file[:type] ...]
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — parent manifest (required); `[file[:type] ...]` — files to attach |
| **Options** | Remote, Packer, `--artifact-type <type>` (required) |
| **Non-interactive** | Outputs the referrer manifest descriptor. |
| **Interactive (TUI)** | Live progress for uploads; tree view of parent→referrer relationship. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `Packer.PackManifestAsync()` with `PackManifestOptions.Subject` |
| **Priority** | P1 |

---

#### `oras discover`

Discover referrers of a manifest.

```
oras discover [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — target manifest (required) |
| **Options** | Remote, `--artifact-type <type>` (filter), `--format` |
| **Non-interactive** | Outputs referrer descriptors as a list (text) or JSON. |
| **Interactive (TUI)** | Tree view of the referrer graph, expandable nodes. |
| **Exit codes** | `0` success (even if no referrers found), `1` error |
| **Library API** | `IRepository.FetchReferrersAsync()` |
| **Priority** | P1 |

**Example output (text — tree format):**
```
localhost:5000/hello:latest
├── application/vnd.example.sbom
│   └── sha256:def456... (1.2 KB)
└── application/vnd.example.signature
    └── sha256:789abc... (256 B)
```

---

#### `oras blob fetch`

Fetch a blob by digest.

```
oras blob fetch [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — blob reference with digest (required) |
| **Options** | Remote, `--output <file>` (write to file; stdout if omitted), `--descriptor` (output descriptor only) |
| **Non-interactive** | Outputs blob content to stdout or file. `--descriptor` outputs JSON descriptor. |
| **Interactive (TUI)** | Progress bar for large blobs; preview for text blobs. |
| **Exit codes** | `0` success, `1` not found or error |
| **Library API** | `IBlobStore.FetchAsync()` |
| **Priority** | P1 |

---

#### `oras blob push`

Push a blob to a registry.

```
oras blob push [options] <reference> <file>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — target reference (required); `<file>` — file to push (required) |
| **Options** | Remote, `--media-type <type>`, `--size <n>` |
| **Non-interactive** | Outputs the blob descriptor with digest and size. |
| **Interactive (TUI)** | Upload progress bar. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `IBlobStore.PushAsync()` |
| **Priority** | P1 |

---

#### `oras blob delete`

Delete a blob from a registry.

```
oras blob delete [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — blob reference with digest (required) |
| **Options** | Remote, `--force` / `-f` (skip confirmation) |
| **Non-interactive** | Requires `--force` or fails. Outputs confirmation message. |
| **Interactive (TUI)** | Confirmation prompt before deletion. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `IDeletable.DeleteAsync()` |
| **Priority** | P1 |

---

#### `oras manifest push`

Push a manifest to a registry.

```
oras manifest push [options] <reference> <file>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — target reference (required); `<file>` — manifest JSON file (required) |
| **Options** | Remote, `--media-type <type>` |
| **Non-interactive** | Outputs the manifest descriptor. |
| **Interactive (TUI)** | Syntax-highlighted preview of manifest before push; confirmation prompt. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `IManifestStore.PushAsync()` / `IReferencePushable.PushAsync()` |
| **Priority** | P1 |

---

#### `oras manifest delete`

Delete a manifest from a registry.

```
oras manifest delete [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — manifest reference (required) |
| **Options** | Remote, `--force` / `-f` (skip confirmation) |
| **Non-interactive** | Requires `--force` or fails. Outputs confirmation message. |
| **Interactive (TUI)** | Confirmation prompt showing manifest details before deletion. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `IDeletable.DeleteAsync()` |
| **Priority** | P1 |

---

#### `oras manifest fetch-config`

Fetch the config blob referenced by a manifest.

```
oras manifest fetch-config [options] <reference>
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — manifest reference (required) |
| **Options** | Remote, Platform, `--output <file>` |
| **Non-interactive** | Outputs config blob JSON to stdout or file. |
| **Interactive (TUI)** | Syntax-highlighted JSON display. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | `IManifestStore.FetchAsync()` → `IBlobStore.FetchAsync(config descriptor)` |
| **Priority** | P1 |

---

### 2.6 P2 Commands (Nice-to-Have)

#### `oras manifest index create`

Create an OCI image index (multi-platform manifest list).

```
oras manifest index create [options] <reference> [<source-reference> ...]
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — target index reference (required); `[<source-reference> ...]` — manifests to include |
| **Options** | Remote, `--annotation`, `--annotation-file` |
| **Non-interactive** | Outputs the index descriptor. |
| **Interactive (TUI)** | Table showing included platforms; confirmation before push. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | Manual construction of `Oci.Index` + `IManifestStore.PushAsync()` |
| **Priority** | P2 |

---

#### `oras manifest index update`

Update an existing OCI image index.

```
oras manifest index update [options] <reference> [--add <ref>] [--remove <ref>] [--merge <ref>]
```

| Aspect | Detail |
|--------|--------|
| **Arguments** | `<reference>` — index reference to update (required) |
| **Options** | Remote, `--add <ref>` (add manifest), `--remove <ref>` (remove manifest), `--merge <ref>` (merge another index), `--annotation`, `--tag <tag>` |
| **Non-interactive** | Outputs the updated index descriptor. |
| **Interactive (TUI)** | Before/after diff view of index; confirmation. |
| **Exit codes** | `0` success, `1` error |
| **Library API** | Fetch existing `Oci.Index` → modify → `IManifestStore.PushAsync()` |
| **Priority** | P2 |

---

### 2.7 Deferred Commands

| Command | Reason | Revisit When |
|---------|--------|-------------|
| `oras backup` | Experimental in Go CLI; no .NET library support | Go CLI promotes to stable |
| `oras restore` | Experimental in Go CLI; no .NET library support | Go CLI promotes to stable |
| `--oci-layout` flag | oras-dotnet lacks OCI layout store | oras-dotnet adds `OciLayout` store |

---

## 3. Interactive Mode (TUI) Requirements

### 3.1 Overview

When `oras` is invoked with no arguments in a TTY terminal, it launches an interactive TUI dashboard powered by Spectre.Console. This is a differentiating feature — the Go CLI simply shows help text.

**TTY detection logic:**
```
if (not Console.IsOutputRedirected AND not Console.IsErrorRedirected AND not --no-tty)
    → launch TUI dashboard
else
    → show help text (standard System.CommandLine behavior)
```

### 3.2 Dashboard Layout

```
┌──────────────────────────────────────────────────────┐
│  oras — OCI Registry As Storage              v0.1.0  │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Connected registries:                               │
│    ● localhost:5000 (logged in)                       │
│    ● ghcr.io (logged in)                             │
│    ○ docker.io (not authenticated)                   │
│                                                      │
│  Quick actions:                                      │
│    [P]ush    [L]ogin     [B]rowse                    │
│    [C]opy    [T]ag       [Q]uit                      │
│                                                      │
├──────────────────────────────────────────────────────┤
│  Recent activity: (from credential store)            │
│    Last login: ghcr.io (2 hours ago)                 │
│    Last push: ghcr.io/myrepo:latest (1 hour ago)     │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### 3.3 Registry Browser

Accessible via the `[B]rowse` action or `oras browse <registry>` command:

1. **Connect** — Select or enter a registry URL; authenticate if needed.
2. **Repository list** — Paginated, searchable list of repositories (`IRegistry.ListRepositoriesAsync()`).
3. **Tag list** — Select a repository to see tags, sorted by last updated.
4. **Manifest inspector** — Select a tag to view:
   - Manifest JSON with syntax highlighting (Spectre.Console `JsonText` or custom rendering)
   - Layer tree: visual tree of layers with media types, sizes, and digests
   - Referrer tree: artifacts referencing this manifest
   - Config blob preview (for image manifests)
5. **Actions from browser** — Pull, copy, tag, delete directly from the browser context.

### 3.4 Push/Pull Progress Visualization

```
Pushing to localhost:5000/myapp:latest

  ████████████████████████████░░░░  87%  hello.txt (1.2 MB)
  ████████████████████████████████ 100%  config.json (256 B) ✓
  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%  data.tar.gz (45 MB)

  Overall: 2/3 layers  │  1.5 MB / 46.5 MB  │  2.3 MB/s  │  ETA: 19s
```

Requirements:
- Per-layer progress bars with filename, size, and percentage
- Overall summary line: layers completed, total bytes, transfer speed, ETA
- Completed layers show ✓ checkmark
- Falls back to line-by-line text output when `--no-tty` or non-TTY detected

### 3.5 Manifest Tree View

When viewing a manifest interactively:

```
manifest (application/vnd.oci.image.manifest.v1+json)
├── config (application/vnd.oci.image.config.v1+json)
│   └── sha256:a1b2c3... (1.5 KB)
├── layers
│   ├── [0] application/vnd.oci.image.layer.v1.tar+gzip
│   │   └── sha256:d4e5f6... (12.3 MB)
│   └── [1] application/vnd.oci.image.layer.v1.tar+gzip
│       └── sha256:789abc... (45.1 MB)
└── referrers
    ├── application/vnd.example.sbom.v1
    │   └── sha256:def012... (4.2 KB)
    └── application/vnd.example.signature
        └── sha256:345678... (256 B)
```

### 3.6 Selection Prompts for Batch Operations

For commands like `tag`, `delete`, or `copy` when used interactively:

- **Multi-select** for batch tagging: select multiple tags to apply
- **Confirmation prompts** for destructive operations (delete) showing what will be deleted
- **Search/filter** in long lists (repos, tags) using Spectre.Console's `TextPrompt` with autocomplete

### 3.7 TUI Priority

The TUI is Sprint 3 work. Sprint 1–2 focuses on the non-interactive CLI. TUI builds on top of the command/service layer established in earlier sprints.

---

## 4. Non-Functional Requirements

### 4.1 Performance Targets

| Metric | Target | Rationale |
|--------|--------|-----------|
| **Cold start (AOT)** | < 50ms | Competitive with Go CLI |
| **Cold start (non-AOT)** | < 200ms | Acceptable for `dotnet tool` usage |
| **Push throughput** | ≥ 90% of available bandwidth | Concurrent blob uploads (configurable) |
| **Pull throughput** | ≥ 90% of available bandwidth | Concurrent blob downloads |
| **Memory (idle)** | < 50 MB | CLI should be lightweight |
| **Memory (transfer)** | < 200 MB for 1 GB artifact | Stream-based processing, not buffering |
| **Binary size (AOT, single-file)** | < 30 MB | Competitive with Go CLI (~20 MB) |

### 4.2 Cross-Platform Behavior

| Platform | Distribution | Credential Helper | Notes |
|----------|-------------|-------------------|-------|
| **Windows x64/ARM64** | `.exe` single-file, `dotnet tool` | `docker-credential-wincred` | Windows Terminal + PowerShell for best TUI |
| **macOS x64/ARM64** | Binary, `dotnet tool` | `docker-credential-osxkeychain` | Full Spectre.Console support |
| **Linux x64/ARM64** | Binary, `dotnet tool` | `docker-credential-secretservice` or `docker-credential-pass` | Standard terminal support |

Path conventions:
- Config: `$DOCKER_CONFIG/config.json` → fallback `~/.docker/config.json`
- Use `Path.Combine()` for all file paths
- Line endings: LF for OCI content, `Environment.NewLine` for CLI output
- All file I/O uses async APIs

### 4.3 Error Handling and Exit Codes

Exit codes follow Go CLI conventions:

| Code | Meaning | Example |
|------|---------|---------|
| `0` | Success | Command completed normally |
| `1` | General error | Auth failure, network error, server error, not found |
| `2` | Argument/usage error | Invalid reference format, missing required argument |

Error output format (to stderr):
```
Error: <concise error message>
Recommendation: <actionable guidance for the user>
```

Error translation rules:
| Library Exception | User Message | Recommendation |
|---|---|---|
| `NotFoundException` | `artifact not found: <ref>` | Check the reference and registry. |
| `AlreadyExistsException` | `artifact already exists: <ref>` | Use a different tag or `--force`. |
| `HttpRequestException` (401/403) | `authentication required` | Run `oras login <registry>` first. |
| `HttpRequestException` (network) | `unable to connect to <host>` | Check network connectivity and registry URL. |
| `TaskCanceledException` | `operation timed out` | Check network connectivity. Retry with `--debug` for details. |

### 4.4 Security

| Concern | Requirement |
|---------|-------------|
| **Credential storage** | Use platform credential helpers when available. Base64 in config.json only as fallback. Never store plaintext passwords outside config.json. |
| **No secrets in output** | Passwords, tokens, and credentials must never appear in stdout, stderr, or `--debug` logs. Mask with `***`. |
| **TLS by default** | HTTPS is the default. `--plain-http` must be explicitly opted into. `--insecure` skips TLS verification (with warning). |
| **stdin password** | `--password-stdin` reads exactly one line, then closes stdin. No echo to terminal. |
| **File permissions** | Config.json written with user-only permissions (0600 on Unix). |
| **Dependency supply chain** | NuGet packages pinned to exact versions via central package management. |
| **No telemetry** | No telemetry or analytics. No network calls except explicit user-requested registry operations. |

### 4.5 Accessibility

| Requirement | Implementation |
|-------------|----------------|
| **Screen reader compatibility** | `--no-tty` mode produces plain text parseable by screen readers |
| **No color-only information** | Status conveyed through text (✓/✗) in addition to color |
| **High-contrast support** | Spectre.Console respects `NO_COLOR` environment variable |
| **Keyboard navigation** | All TUI interactions navigable via keyboard |

---

## 5. Testing Strategy

### 5.1 Testing Pyramid

```
          ┌─────────────┐
          │  E2E Tests   │   Few — full binary invocation against real registry
          │  (manual)    │
         ┌┴─────────────┴┐
         │  Integration   │   testcontainers-dotnet + real OCI registry
         │  Tests         │   (one container per test class)
        ┌┴───────────────┴┐
        │   Unit Tests     │   Mocked services, no I/O, fast
        │   (majority)     │
        └──────────────────┘
```

### 5.2 Unit Test Scope

**Location:** `test/oras.Tests/` (and expanded as solution grows)

| Layer | What Gets Tested | Mocking Strategy |
|-------|-----------------|------------------|
| **Commands** | Argument parsing, option binding, validation | Mock services via DI |
| **Services** | Business logic, error translation, progress reporting | Mock oras-dotnet interfaces (`IRepository`, `IBlobStore`, etc.) |
| **Credentials** | Config.json read/write, credential helper invocation | File system abstraction, mock process execution |
| **Output** | `TextFormatter` and `JsonFormatter` produce correct output | In-memory `IAnsiConsole` (Spectre.Console testing support) |
| **FileRef** | Reference parsing (`registry/repo:tag@digest`) | Pure functions, no mocking needed |

Unit test conventions:
- One test class per production class
- Method naming: `MethodName_Scenario_ExpectedResult`
- Use `xUnit` with `[Fact]` and `[Theory]`
- No real network or file I/O in unit tests

### 5.3 Integration Test Scope

**Location:** `test/oras.IntegrationTests/` (to be created)

| Test Category | What Gets Tested | Infrastructure |
|---------------|-----------------|----------------|
| **Push/Pull roundtrip** | Push files, pull them back, verify content matches | testcontainers: OCI distribution registry |
| **Login/Logout** | Credential storage and retrieval | testcontainers: registry with auth enabled |
| **Copy** | Cross-repository copy within same registry | testcontainers: OCI distribution registry |
| **Tag/Resolve** | Tag creation and digest resolution | testcontainers: OCI distribution registry |
| **Blob operations** | Push/fetch/delete individual blobs | testcontainers: OCI distribution registry |
| **Manifest operations** | Fetch/push/delete manifests | testcontainers: OCI distribution registry |
| **Discover** | Referrer discovery after attach | testcontainers: OCI distribution registry |
| **Error scenarios** | Auth failures, not-found, network errors | testcontainers: registry with various configs |

Integration test conventions:
- Use `testcontainers-dotnet` with `ghcr.io/oci-playground/registry:latest` (or `registry:2`)
- One container per test class (shared via `IAsyncLifetime`)
- Tests are independent and can run in parallel across classes
- Integration tests tagged with `[Trait("Category", "Integration")]` for selective execution

### 5.4 What Gets Tested at Each Level

| Feature | Unit | Integration |
|---------|:----:|:-----------:|
| Reference parsing | ✅ | — |
| Option validation | ✅ | — |
| Error message formatting | ✅ | — |
| Service orchestration logic | ✅ | — |
| Output formatting (text/JSON) | ✅ | — |
| Credential config.json parsing | ✅ | — |
| Push/pull data integrity | — | ✅ |
| Auth flow (challenge/token) | — | ✅ |
| Concurrent upload/download | — | ✅ |
| Cross-registry copy | — | ✅ |
| Manifest index create/update | — | ✅ |
| Exit codes (full binary) | — | ✅ |
| TUI rendering (visual) | ✅ (snapshot) | — |

---

## 6. CI/CD and Release

### 6.1 PR Gate Requirements

Every pull request must pass:

| Gate | Tool | Requirement |
|------|------|-------------|
| **Build** | `dotnet build` | Zero errors, zero warnings (`TreatWarningsAsErrors`) |
| **Unit Tests** | `dotnet test --filter Category!=Integration` | 100% pass |
| **Integration Tests** | `dotnet test --filter Category=Integration` | 100% pass (Docker required on runner) |
| **Code Style** | `dotnet format --verify-no-changes` | No formatting violations |
| **Static Analysis** | Roslyn analyzers (`AnalysisLevel: latest-all`) | No warnings |

GitHub Actions workflow: `.github/workflows/ci.yml`

```yaml
# Triggers: push to main, pull_request
# Matrix: ubuntu-latest, windows-latest, macos-latest
# Steps: restore → build → test (unit) → test (integration) → format check
```

### 6.2 Release Pipeline

**Trigger:** Git tag `v*` (e.g., `v0.1.0`)

**Artifacts produced:**

| Artifact | RID | Format |
|----------|-----|--------|
| `oras-win-x64.exe` | `win-x64` | Self-contained single-file (AOT) |
| `oras-win-arm64.exe` | `win-arm64` | Self-contained single-file (AOT) |
| `oras-linux-x64` | `linux-x64` | Self-contained single-file (AOT) |
| `oras-linux-arm64` | `linux-arm64` | Self-contained single-file (AOT) |
| `oras-osx-x64` | `osx-x64` | Self-contained single-file (AOT) |
| `oras-osx-arm64` | `osx-arm64` | Self-contained single-file (AOT) |


Release workflow: `.github/workflows/release.yml`
```
Tag push → Build matrix (6 RIDs) → Publish AOT binaries → Create GitHub Release
         → Build & deploy docs to GitHub Pages
```

### 6.3 GitHub Pages Documentation

**Source:** `docs/` directory
**Generator:** docfx or custom markdown-to-HTML pipeline
**Content:**
- Installation guide (binaries, dotnet tool, from source)
- Command reference (generated from System.CommandLine help output + PRD)
- Tutorials (push/pull workflow, CI/CD integration, TUI guide)
- Architecture overview
- Contributing guide

---

## 7. Work Breakdown

### 7.1 Team Roster

| Name | Role | Strengths |
|------|------|-----------|
| **Dallas** | Core Dev | .NET, System.CommandLine, OCI library integration |
| **Bishop** | TUI Dev | Spectre.Console, interactive UI, rendering |
| **Hicks** | Tester | xUnit, testcontainers-dotnet, test architecture |
| **Vasquez** | DevOps | GitHub Actions, CI/CD, release pipelines, AOT builds |

### 7.2 Sprint 1 — Core Foundation (Weeks 1–2)

Goal: Project structure, base command framework, credential store, and the core push/pull/login/logout workflow end-to-end.

| ID | Title | Description | Assignee | Dependencies | Priority |
|----|-------|-------------|----------|-------------- |----------|
| **S1-01** | Project restructure | Rename `src/oras` → `src/Oras.Cli`, set up `Oras.Cli.csproj` with correct package refs. Create `Options/`, `Services/`, `Commands/`, `Credentials/`, `Output/` directory structure per design review. Configure `Directory.Packages.props` for central package management. | Dallas | — | P0 |
| **S1-02** | Shared option infrastructure | Implement `CommonOptions`, `RemoteOptions`, `TargetOptions`, `PackerOptions`, `FormatOptions`, `PlatformOptions` as composable System.CommandLine `Option<T>` groups with `ApplyTo(Command)` pattern. | Dallas | S1-01 | P0 |
| **S1-03** | IOutputFormatter abstraction | Define `IOutputFormatter` interface. Implement `TextFormatter` (Spectre.Console for TTY, plain text fallback) and `JsonFormatter`. Wire `--format` global option. | Bishop | S1-01 | P0 |
| **S1-04** | Service layer scaffold | Create `RegistryService` (registry/repo client factory with credential integration), `PushService`, `PullService`, `CredentialService` interfaces and base implementations. Define DI registration pattern. | Dallas | S1-01 | P0 |
| **S1-05** | Docker credential store | Implement `DockerConfigStore`: read/write `~/.docker/config.json` (`auths` field, `credsStore`, `credHelpers`). Implement `NativeCredentialHelper`: shell out to `docker-credential-*` protocol (store, get, erase, list). Implement `CredentialProviderFactory` to bridge to oras-dotnet `ICredentialProvider`. | Dallas | S1-01 | P0 |
| **S1-06** | Login command | Implement `oras login <registry>`. Non-interactive: require `-u`/`-p` or `--password-stdin`. Interactive: prompt for missing credentials. Validate against registry. Store via credential service. | Dallas | S1-02, S1-05 | P0 |
| **S1-07** | Logout command | Implement `oras logout <registry>`. Remove credentials from store. Silent success. | Dallas | S1-05 | P0 |
| **S1-08** | Version command | Implement `oras version`. Display CLI version, library version, runtime, platform, commit SHA. | Dallas | S1-01 | P0 |
| **S1-09** | Push command | Implement `oras push <reference> [files...]`. File→descriptor mapping, `Packer.PackManifestAsync()`, `CopyAsync()` to registry. Support `--artifact-type`, `--annotation`, `--concurrency`. Progress reporting via `IOutputFormatter`. | Dallas | S1-02, S1-03, S1-04 | P0 |
| **S1-10** | Pull command | Implement `oras pull <reference>`. Resolve manifest, fetch layers to `--output` dir. Support `--concurrency`, `--keep-old-files`, `--platform`. Progress reporting. | Dallas | S1-02, S1-03, S1-04 | P0 |
| **S1-11** | Error handling middleware | Implement global exception handler: catch oras-dotnet exceptions, `HttpRequestException`, `TaskCanceledException`. Translate to user-friendly `Error:` / `Recommendation:` format. Map to exit codes (0, 1, 2). | Dallas | S1-01 | P0 |
| **S1-12** | Unit test infrastructure | Set up `test/Oras.Cli.Tests/` project. Configure xUnit, mocking library. Create test helpers for command invocation, output capture. Write initial tests for option parsing, credential store, formatters. | Hicks | S1-01 | P0 |
| **S1-13** | Integration test infrastructure | Set up `test/Oras.Cli.IntegrationTests/` project. Configure testcontainers-dotnet with OCI registry. Create shared fixture for registry container lifecycle. Write push/pull roundtrip integration test. | Hicks | S1-09, S1-10 | P0 |
| **S1-14** | CI pipeline (PR gates) | Create `.github/workflows/ci.yml`: build matrix (ubuntu, windows, macos), unit tests, integration tests, format check. Configure Docker-in-Docker for integration tests on runners. | Vasquez | S1-12, S1-13 | P0 |
| **S1-15** | Progress renderer | Implement `ProgressRenderer` using `AnsiConsole.Progress()` for push/pull. Hook into library callbacks (`CopyGraphOptions.PreCopy`/`PostCopy`). Plain text fallback for non-TTY. | Bishop | S1-03 | P0 |

### 7.3 Sprint 2 — Full Command Parity (Weeks 3–4)

Goal: Implement all remaining P0 and P1 commands. Complete non-interactive CLI feature set.

| ID | Title | Description | Assignee | Dependencies | Priority |
|----|-------|-------------|----------|-------------- |----------|
| **S2-01** | Tag command | Implement `oras tag <source> <tag> [<tag>...]`. Multiple tags in one invocation. | Dallas | S1-04 | P0 |
| **S2-02** | Resolve command | Implement `oras resolve <reference>`. Output digest. Support `--platform` for index resolution. | Dallas | S1-04 | P0 |
| **S2-03** | Copy command | Implement `oras copy <src> <dst>`. Support `--recursive`, `--concurrency`, `--platform`. Cross-registry and same-registry copy. Progress reporting. | Dallas | S1-04, S1-15 | P0 |
| **S2-04** | Repo ls command | Implement `oras repo ls <registry>`. Paginated output. Support `--last` marker. | Dallas | S1-04 | P0 |
| **S2-05** | Repo tags command | Implement `oras repo tags <reference>`. Paginated output. Support `--last` marker. | Dallas | S1-04 | P0 |
| **S2-06** | Manifest fetch command | Implement `oras manifest fetch <reference>`. Support `--descriptor`, `--output`, `--pretty`, `--platform`. | Dallas | S1-04 | P0 |
| **S2-07** | Attach command | Implement `oras attach <reference> [files...]`. Set subject on packed manifest. Support `--artifact-type` (required). | Dallas | S1-09 | P1 |
| **S2-08** | Discover command | Implement `oras discover <reference>`. Tree-format output for text. JSON output. Support `--artifact-type` filter. | Dallas | S1-04 | P1 |
| **S2-09** | Blob fetch command | Implement `oras blob fetch <reference>`. Output to stdout or `--output` file. Support `--descriptor` mode. | Dallas | S1-04 | P1 |
| **S2-10** | Blob push command | Implement `oras blob push <reference> <file>`. Output blob descriptor. Support `--media-type`. | Dallas | S1-04 | P1 |
| **S2-11** | Blob delete command | Implement `oras blob delete <reference>`. Require `--force` in non-interactive. Confirmation prompt in interactive. | Dallas | S1-04 | P1 |
| **S2-12** | Manifest push command | Implement `oras manifest push <reference> <file>`. Read manifest JSON from file. Support `--media-type`. | Dallas | S1-04 | P1 |
| **S2-13** | Manifest delete command | Implement `oras manifest delete <reference>`. Require `--force` in non-interactive. Confirmation prompt in interactive. | Dallas | S1-04 | P1 |
| **S2-14** | Manifest fetch-config command | Implement `oras manifest fetch-config <reference>`. Two-step fetch: manifest → config blob. | Dallas | S2-06 | P1 |
| **S2-15** | Manifest index create | Implement `oras manifest index create <reference> [refs...]`. Build `Oci.Index` manually. | Dallas | S2-06 | P2 |
| **S2-16** | Manifest index update | Implement `oras manifest index update <reference>`. Support `--add`, `--remove`, `--merge`. | Dallas | S2-15 | P2 |
| **S2-17** | Sprint 2 unit tests | Unit tests for all Sprint 2 commands. Test argument parsing, service calls, error cases, output formatting for each command. | Hicks | S2-01 through S2-16 | P0 |
| **S2-18** | Sprint 2 integration tests | Integration tests: tag roundtrip, copy between repos, discover after attach, blob operations, manifest operations. All against testcontainers registry. | Hicks | S2-01 through S2-16 | P0 |

### 7.4 Sprint 3 — TUI / Interactive Mode (Weeks 5–6)

Goal: Build the interactive TUI dashboard, registry browser, and enhanced interactive experiences on top of the command layer.

| ID | Title | Description | Assignee | Dependencies | Priority |
|----|-------|-------------|----------|-------------- |----------|
| **S3-01** | TUI dashboard shell | Implement the main dashboard screen: app header, connected registries list (from credential store), quick actions menu, keyboard navigation. Launch when `oras` invoked with no args in TTY. | Bishop | S1-05, S1-03 | P1 |
| **S3-02** | Registry browser — connect | Implement registry connection flow: select from stored credentials or enter new URL. Auth prompt if needed. | Bishop | S3-01, S1-06 | P1 |
| **S3-03** | Registry browser — repo list | Implement paginated, searchable repository list using `IRegistry.ListRepositoriesAsync()`. Spectre.Console `SelectionPrompt` with search. | Bishop | S3-02 | P1 |
| **S3-04** | Registry browser — tag list | Implement tag list for selected repository. Sortable, searchable. | Bishop | S3-03 | P1 |
| **S3-05** | Manifest inspector | Implement manifest viewer: syntax-highlighted JSON, layer tree view, referrer tree, config preview. Use Spectre.Console `Tree`, `JsonText`, and `Panel` widgets. | Bishop | S3-04 | P1 |
| **S3-06** | Browser actions | Implement contextual actions from browser: pull selected tag, copy, tag, delete. Reuse command services. | Bishop | S3-05, S2-* | P1 |
| **S3-07** | Enhanced push/pull progress | Upgrade progress visualization: per-layer bars, overall summary, transfer speed, ETA. Use `AnsiConsole.Live()` for real-time updates. | Bishop | S1-15 | P1 |
| **S3-08** | Selection prompts | Implement multi-select for batch tag, confirmation dialogs for delete, search/filter in lists. Consistent prompt UX across all interactive commands. | Bishop | S3-01 | P1 |
| **S3-09** | TUI unit tests | Snapshot tests for TUI components using Spectre.Console test infrastructure (`IAnsiConsole` mock). Test dashboard rendering, tree views, progress output. | Hicks | S3-01 through S3-08 | P1 |

### 7.5 Sprint 4 — Polish, Docs, CI/CD, Release (Weeks 7–8)

Goal: Production readiness. Documentation, release pipeline, performance optimization, and polish.

| ID | Title | Description | Assignee | Dependencies | Priority |
|----|-------|-------------|----------|-------------- |----------|
| **S4-01** | Native AOT configuration | Configure AOT publish profiles for all 6 RIDs. Resolve trimming warnings. Verify all commands work under AOT. Add `rd.xml` / trimmer directives as needed. | Vasquez | S2-* | P0 |
| **S4-02** | Release pipeline | Create `.github/workflows/release.yml`: tag-triggered build matrix, AOT publish, GitHub Release with binaries. | Vasquez | S4-01 | P0 |
| **S4-03** | Shell completions | Enable System.CommandLine tab completion for bash, zsh, PowerShell, fish. Document installation. | Dallas | S2-* | P1 |
| **S4-04** | Performance benchmarks | Benchmark cold start (AOT vs non-AOT), push/pull throughput, memory usage. Compare with Go CLI. Document results. Optimize if targets not met. | Vasquez | S4-01 | P1 |
| **S4-05** | Installation documentation | Write installation guide: binary download, `dotnet tool install`, build from source. Platform-specific instructions. | Dallas | S4-02 | P0 |
| **S4-06** | Command reference docs | Generate command reference documentation from System.CommandLine help + PRD content. Markdown format for GitHub Pages. | Dallas | S2-* | P0 |
| **S4-07** | Tutorial content | Write tutorials: getting started, push/pull workflow, CI/CD integration example, TUI guide. | Bishop | S3-* | P1 |
| **S4-08** | GitHub Pages setup | Configure docfx or static site generator. Set up `.github/workflows/docs.yml` for automatic deployment on main push. | Vasquez | S4-05, S4-06 | P1 |
| **S4-09** | Security hardening review | Audit credential store implementation. Verify no secrets in logs. Verify file permissions. Review dependency supply chain. | Hicks | S1-05 | P0 |
| **S4-10** | Cross-platform validation | Run full test suite on Windows, macOS (Intel + ARM), Linux (x64 + ARM64). Fix platform-specific issues. Verify credential helpers on each platform. | Hicks | S4-01 | P0 |
| **S4-11** | Error message audit | Review all error messages for clarity and actionability. Ensure every error has a recommendation. Verify exit codes match Go CLI conventions. | Hicks | S2-* | P1 |
| **S4-12** | README and contributing guide | Write project README (badges, features, installation, quick start). Write CONTRIBUTING.md (build from source, test, PR process). | Dallas | S4-05 | P0 |

### 7.6 Sprint Summary

| Sprint | Duration | Theme | Key Deliverable |
|--------|----------|-------|-----------------|
| **Sprint 1** | Weeks 1–2 | Core Foundation | Working push/pull/login/logout with tests and CI |
| **Sprint 2** | Weeks 3–4 | Command Parity | All 20+ commands implemented and tested |
| **Sprint 3** | Weeks 5–6 | Interactive TUI | Dashboard, registry browser, enhanced progress |
| **Sprint 4** | Weeks 7–8 | Release Ready | AOT binaries, docs, release pipeline, hardening |

### 7.7 Dependency Graph (Critical Path)

```
S1-01 (project structure)
  ├── S1-02 (options) ──► S1-06 (login) ──► S2-* (all commands)
  ├── S1-03 (formatters) ──► S1-15 (progress) ──► S3-07 (enhanced progress)
  ├── S1-04 (services) ──► S1-09 (push) ──► S1-13 (integration tests)
  ├── S1-05 (credentials) ──► S1-06 (login) ──► S3-01 (TUI dashboard)
  └── S1-11 (errors) ──► S2-* (all commands)

S2-* (all commands) ──► S3-06 (browser actions) ──► S4-01 (AOT)
S1-12 (test infra) ──► S2-17 (sprint 2 tests) ──► S4-10 (cross-platform)
S1-14 (CI) ──► S4-02 (release pipeline)
```

---

## Appendix A: Go CLI Flag Parity Reference

This table maps Go CLI flags to their .NET equivalents for verification during implementation.

| Go Flag | .NET Option | Commands | Notes |
|---------|-------------|----------|-------|
| `--debug` | `--debug` | All | Verbose to stderr |
| `--no-tty` | `--no-tty` | All | Force non-interactive |
| `-u, --username` | `-u, --username` | login, Remote group | |
| `-p, --password` | `-p, --password` | login, Remote group | |
| `--password-stdin` | `--password-stdin` | login, Remote group | |
| `--insecure` | `--insecure` | Remote group | Skip TLS verify |
| `--plain-http` | `--plain-http` | Remote group | HTTP instead of HTTPS |
| `--ca-file` | `--ca-file` | Remote group | CA certificate |
| `--registry-config` | `--registry-config` | Remote group | Config file path |
| `--concurrency` | `--concurrency` | push, pull, copy | Default: 5 |
| `--artifact-type` | `--artifact-type` | push, attach | |
| `-a, --annotation` | `-a, --annotation` | push, attach | `key=value` pairs |
| `--annotation-file` | `--annotation-file` | push, attach | JSON file |
| `--export-manifest` | `--export-manifest` | push, attach | Export to file |
| `--image-spec` | `--image-spec` | push, attach | `v1.0` or `v1.1` |
| `--platform` | `--platform` | pull, copy, manifest fetch, resolve | `os/arch` |
| `--format` | `--format` | Many | `text`, `json` (no `go-template`) |
| `-o, --output` | `-o, --output` | pull, blob fetch, manifest fetch | Output file/dir |
| `--descriptor` | `--descriptor` | manifest fetch, blob fetch | Descriptor only |
| `--pretty` | `--pretty` | manifest fetch | Pretty-print JSON |
| `-r, --recursive` | `-r, --recursive` | copy | Recursive copy |
| `-f, --force` | `-f, --force` | blob delete, manifest delete | Skip confirmation |
| `--keep-old-files` | `--keep-old-files` | pull | Don't overwrite |
| `--oci-layout` | *(deferred)* | — | No .NET library support |

---

## Appendix B: OCI Reference Format

All commands accepting `<reference>` use this format:

```
[registry/]repository[:tag|@digest]
```

Examples:
- `localhost:5000/myrepo:latest`
- `ghcr.io/myorg/myartifact:v1.0`
- `docker.io/library/alpine@sha256:abc123...`

The CLI must parse and validate references before passing to the library. Invalid references produce exit code 2 with a clear error message.

---

*This PRD is the authoritative specification for the oras .NET CLI. It feeds directly into sprint planning and work assignment. All implementation decisions should reference this document and the architecture design review (`docs/design-review.md`).*
