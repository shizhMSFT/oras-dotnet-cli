---
title: "Migration Guide"
layout: default
nav_order: 7
description: "Switching from the Go oras CLI to the .NET oras CLI"
---

# Migration Guide
{: .fs-9 }

Switching from the Go `oras` CLI to the .NET `oras` CLI.
{: .fs-6 .fw-300 }

---

## Introduction

The .NET `oras` CLI is a ground-up reimagining of the [Go-based oras CLI](https://github.com/oras-project/oras), built on .NET 10 with the official [OrasProject.Oras](https://github.com/oras-project/oras-dotnet) library.

### Why switch?

- **Rich terminal experience** — Spectre.Console powers colored tables, tree views, and real-time progress bars out of the box.
- **Interactive TUI dashboard** — Browse registries, inspect manifests, and manage artifacts without memorizing commands.
- **.NET ecosystem integration** — Embed OCI artifact workflows directly in .NET applications via NuGet.
- **Cross-platform single binaries** — Self-contained executables for Windows, macOS (Intel & ARM), and Linux (x64 & ARM64) with no runtime dependency.

### Alpha preview

{: .warning }
> The .NET CLI is currently at **v0.1.0**. Some commands have stubbed implementations pending full library integration. See [Known Limitations](#known-limitations) below.

---

## Installation Side-by-Side

Both CLIs can coexist on the same machine. If you install the .NET CLI to a different binary name (e.g., `oras-dotnet`), no conflicts arise.

### Go CLI

```bash
# Homebrew (macOS/Linux)
brew install oras

# Or download from GitHub Releases
curl -LO https://github.com/oras-project/oras/releases/download/v1.2.0/oras_1.2.0_linux_amd64.tar.gz
tar -xzf oras_1.2.0_linux_amd64.tar.gz
sudo mv oras /usr/local/bin/
```

### .NET CLI

```bash
# Download a self-contained binary
curl -LO https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.0/oras-linux-x64.tar.gz
tar -xzf oras-linux-x64.tar.gz
chmod +x oras-linux-x64
sudo mv oras-linux-x64 /usr/local/bin/oras

# Or build from source
git clone https://github.com/shizhMSFT/oras-dotnet-cli.git
cd oras-dotnet-cli
dotnet publish src/Oras.Cli -c Release -r linux-x64 --self-contained
```

See the [Installation Guide](installation) for full platform-specific instructions.

---

## Command Comparison Table

All Go CLI commands have corresponding .NET CLI commands. The command names and argument positions are identical by design.

| Go CLI Command | .NET CLI Command | Status | Notes |
|:---------------|:-----------------|:------:|:------|
| `oras push <ref> [files...]` | `oras push <ref> [files...]` | ⚠️ Partial | File validation and blob pushing work; manifest packing awaits library API integration |
| `oras pull <ref>` | `oras pull <ref>` | ⚠️ Partial | Reference parsing and repository creation work; blob fetching awaits library API integration |
| `oras attach <ref> [files...]` | `oras attach <ref> [files...]` | 🔲 Stub | Awaiting `Packer.PackManifestAsync()` integration |
| `oras discover <ref>` | `oras discover <ref>` | 🔲 Stub | Awaiting `IRepository.FetchReferrersAsync()` integration |
| `oras copy <src> <dst>` | `oras copy <src> <dst>` | ✅ Full | Enhanced with progress, source auth, reference validation |
| N/A | `oras backup <ref>` | 🆕 New | .NET CLI exclusive — save artifacts to local OCI layout or tar archive |
| N/A | `oras restore <path> <dst>` | 🆕 New | .NET CLI exclusive — restore local backups to any registry |
| `oras logout <registry>` | `oras logout <registry>` | ✅ Full | Credential removal from Docker config.json |
| `oras tag <ref> <tag> [tags...]` | `oras tag <ref> <tag> [tags...]` | 🔲 Stub | Awaiting `ITaggable.TagAsync()` integration |
| `oras resolve <ref>` | `oras resolve <ref>` | 🔲 Stub | Awaiting `IResolvable.ResolveAsync()` integration |
| `oras repo ls <registry>` | `oras repo ls <registry>` | 🔲 Stub | Awaiting `IRegistry.ListRepositoriesAsync()` integration |
| `oras repo tags <ref>` | `oras repo tags <ref>` | 🔲 Stub | Awaiting `ITagListable.TagsAsync()` integration |
| `oras manifest fetch <ref>` | `oras manifest fetch <ref>` | 🔲 Stub | Awaiting `IManifestStore.FetchAsync()` integration |
| `oras manifest push <ref> <file>` | `oras manifest push <ref> <file>` | 🔲 Stub | Awaiting `IManifestStore.PushAsync()` integration |
| `oras manifest delete <ref>` | `oras manifest delete <ref>` | 🔲 Stub | Awaiting `IDeletable.DeleteAsync()` integration |
| `oras manifest fetch-config <ref>` | `oras manifest fetch-config <ref>` | 🔲 Stub | Two-step fetch (manifest → config blob) |
| `oras blob fetch <ref>` | `oras blob fetch <ref>` | 🔲 Stub | Awaiting `IBlobStore.FetchAsync()` integration |
| `oras blob push <ref> <file>` | `oras blob push <ref> <file>` | 🔲 Stub | Awaiting `IBlobStore.PushAsync()` integration |
| `oras blob delete <ref>` | `oras blob delete <ref>` | 🔲 Stub | Awaiting `IDeletable.DeleteAsync()` integration |
| `oras version` | `oras version` | ✅ Full | Shows CLI version, library version, .NET runtime, OS/platform |

**Legend:** ✅ Fully implemented — ⚠️ Partially implemented — 🔲 Command scaffolded (stubbed)

---

## Key Differences

### Flag syntax

The .NET CLI preserves the same flag names as the Go CLI. Standard POSIX conventions apply in both:

```bash
# Identical in both CLIs
oras push ghcr.io/myorg/repo:v1 --artifact-type application/vnd.example
oras manifest fetch ghcr.io/myorg/repo:v1 --pretty --descriptor
oras copy src dst --recursive --concurrency 3
```

### Output format

The Go CLI supports `--format go-template` for custom output formatting. The .NET CLI does **not** support Go templates.

| Format | Go CLI | .NET CLI |
|:-------|:------:|:--------:|
| `--format text` | ✅ | ✅ |
| `--format json` | ✅ | ✅ |
| `--format go-template` | ✅ | ❌ |

{: .note }
> Use `--format json` for machine-readable output. Pipe through [`jq`](https://jqlang.github.io/jq/) for custom field extraction — this covers most `go-template` use cases.

### Exit codes

Exit codes are identical by design:

| Code | Meaning |
|:----:|:--------|
| `0` | Success |
| `1` | General error |
| `2` | Argument/usage error |

### Credential storage

Both CLIs use Docker-compatible credential storage (`~/.docker/config.json`) and support the Docker credential helper protocol. Existing Docker or Go CLI credentials work automatically with the .NET CLI — no re-authentication needed.

### Rich terminal output

The .NET CLI uses [Spectre.Console](https://spectreconsole.net/) for terminal rendering. Compared to the Go CLI's plain text output, you'll see:

- **Progress bars** with transfer speed and ETA during push/pull operations
- **Colored tables** for discover, repo ls, and repo tags output
- **Tree views** for manifest and referrer hierarchies
- **Styled error messages** with context and suggestions

All rich output degrades gracefully in non-interactive environments (CI/CD pipes).

---

## What's New in .NET CLI

These features have no equivalent in the Go CLI.

### Interactive TUI dashboard

Run `oras` with no arguments to launch an interactive terminal dashboard:

```bash
# Launch the TUI
oras

# Open directly to a registry
oras --registry ghcr.io

# Browse a specific repository
oras --registry ghcr.io --repository myorg/myrepo
```

The TUI provides:
- **Dashboard** — credential store status, recent operations, quick actions
- **Registry browser** — hierarchical navigation of repositories, tags, and manifests
- **Keyboard-driven** — full keyboard navigation with vim-style shortcuts

See the [TUI Guide](tui-guide) for details.

### Native .NET integration

The .NET CLI is built on the official [OrasProject.Oras](https://www.nuget.org/packages/OrasProject.Oras) NuGet package. You can embed OCI artifact operations directly in .NET applications:

```csharp
// Use the same library the CLI uses
dotnet add package OrasProject.Oras
```

### Shell completions

Built-in tab completion for bash, zsh, PowerShell, and fish. See [Shell Completions](shell-completions).

### Backup and Restore Commands

The .NET CLI includes two exclusive commands not available in the Go CLI:

- **`oras backup`** — Save artifacts from a registry to a local OCI layout directory or tar archive for disaster recovery and air-gapped environments
- **`oras restore`** — Push artifacts from a local backup to any OCI-compliant registry

---

## Known Limitations

The v0.1.0 release has the following limitations compared to the Go CLI:

### Stubbed commands

The following commands are scaffolded with correct argument parsing and help text but throw `NotImplementedException` when executed against a registry. They will be completed as the OrasProject.Oras library integration progresses:

- `oras attach`
- `oras discover`
- `oras tag`
- `oras resolve`
- `oras repo ls`
- `oras repo tags`
- `oras manifest fetch`
- `oras manifest push`
- `oras manifest delete`
- `oras manifest fetch-config`
- `oras blob fetch`
- `oras blob push`
- `oras blob delete`

### Partially implemented commands

- **`oras push`** — File validation and blob uploading work; manifest packing requires library API integration.
- **`oras pull`** — Reference parsing and repository creation work; blob downloading requires library API integration.

### Missing features

| Feature | Go CLI | .NET CLI (Alpha) | Planned |
|:--------|:------:|:----------------:|:-------:|
| `--oci-layout` (local OCI layout) | ✅ | ❌ | Yes |
| `--format go-template` | ✅ | ❌ | No (use `--format json` + `jq`) |
| `--distribution-spec` flag | ✅ | ❌ | Yes |
| `--from-oci-layout` / `--to-oci-layout` | ✅ | ❌ | Yes |

---

## Quick Migration Checklist

Use this checklist when switching from the Go CLI to the .NET CLI:

- [ ] **Install the .NET CLI** — see [Installation Guide](installation)
- [ ] **Verify installation** — run `oras version` and confirm output
- [ ] **Credentials carry over** — both CLIs use `~/.docker/config.json`; no re-login needed
- [ ] **Update scripts** — replace `--format go-template='{{.digest}}'` with `--format json` piped through `jq`
- [ ] **Test core workflows** — `login`, `logout`, and `version` are fully functional today
- [ ] **Check command status** — review the [Command Comparison Table](#command-comparison-table) for your specific commands
- [ ] **Try the TUI** — run `oras` to explore the interactive dashboard
- [ ] **Report issues** — file bugs at [GitHub Issues](https://github.com/shizhMSFT/oras-dotnet-cli/issues)

---

{: .text-center .fs-3 }
Ready to install? Head to the [Installation Guide](installation) for platform-specific instructions.

{: .tip }
> **Coexistence strategy:** Keep the Go CLI installed for production use while evaluating the .NET CLI. Install the .NET binary as `oras-dotnet` to avoid PATH conflicts, then alias it to `oras` once you're ready to switch.
