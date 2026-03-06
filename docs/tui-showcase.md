---
title: TUI Showcase
layout: default
nav_order: 6
description: "Visual showcase of the interactive Terminal UI features powered by Spectre.Console"
---

# TUI Showcase
{: .fs-8 }

A visual tour of the ORAS .NET CLI's interactive terminal interface — powered by [Spectre.Console](https://spectreconsole.net/).
{: .fs-5 .fw-300 }

Every screen below is a faithful reproduction of real TUI output. Launch it yourself with `oras tui`.
{: .text-grey-dk-000 }

---

## Dashboard
{: .text-yellow-300 }

The main entry point when you run `oras` with no arguments in an interactive terminal. Shows connected registries, auth status, and a quick-actions menu.

```text
╔══════════════════════════════════════════════════════════════╗
║  oras — OCI Registry As Storage                             ║
║  Version 0.1.0                                              ║
╚══════════════════════════════════════════════════════════════╝

╭──────────────────────────────────────────────────────────────╮
│              Connected Registries                            │
├──────────────────────────────────────────────────────────────┤
│  ghcr.io                          ● logged in               │
│  localhost:5000                    ● logged in               │
│  myregistry.azurecr.io            ○ not authenticated        │
╰──────────────────────────────────────────────────────────────╯

  Select an action:
  ❯ Browse Registry
    Login
    Push Artifact
    Pull Artifact
    Copy Artifact
    Tag Artifact
    Quit

  (Move up and down to reveal more options)
```

**How to launch:**

```bash
# Auto-launches when stdin is a TTY and no arguments are provided
oras

# Or explicitly
oras tui
```

---

## Registry Browser
{: .text-yellow-300 }

Hierarchical, searchable repository and tag browser. Select a registry from stored credentials or enter a new URL, then drill into repositories and tags.

```text
  ✓ Connected to ghcr.io

  Repositories in ghcr.io (Total: 12):
  Type to search...
  ❯ myorg/webapp
    myorg/api-service
    myorg/ml-model
    myorg/helm-charts
    myorg/signatures
    myorg/sbom-store
    team/frontend
    team/backend
    team/infra-configs
    demo/hello-oras
    (Move up and down to reveal more options)
```

After selecting a repository:

```text
  Tags for myorg/webapp (Total: 8):
  Type to search...
  ❯ latest
    v2.1.0
    v2.0.0
    v1.5.3
    v1.5.2
    v1.0.0
    sha-a1b2c3d
    nightly-20260305
    Back to repository list
```

**How to launch:**

```bash
# Browse from the dashboard
oras tui
# → Select "Browse Registry"

# Or jump directly to a registry
oras tui --registry ghcr.io
```

---

## Push / Pull Progress
{: .text-yellow-300 }

Real-time per-layer progress bars with transfer speed, percentage, and ETA. Uses a custom `TransferSpeedColumn` for human-readable bandwidth display.

### Pushing

```text
  Pushing to ghcr.io/myorg/webapp:v2.1.0 (3 layers)

  ✓ sha256:a3ed95  config.json                ━━━━━━━━━━━━━━━━━━━━  100%   1.5 KB   --
  ✓ sha256:e1b2f3  app-binary.tar.gz          ━━━━━━━━━━━━━━━━━━━━  100%  45.6 MB   --
    sha256:c4d5e6  static-assets.tar.gz       ━━━━━━━━━━━━━━━━░░░░   78%  12.3 MB   24.5 MB/s  0:03
  Pushing                                     ━━━━━━━━━━━━━━━░░░░░   66%           2/3 layers
```

### Pulling

```text
  Pulling from ghcr.io/myorg/webapp:v2.1.0 (3 layers)

  ✓ sha256:a3ed95  config.json                ━━━━━━━━━━━━━━━━━━━━  100%   1.5 KB   --
  ✓ sha256:e1b2f3  app-binary.tar.gz          ━━━━━━━━━━━━━━━━━━━━  100%  45.6 MB   --
  ✓ sha256:c4d5e6  static-assets.tar.gz       ━━━━━━━━━━━━━━━━━━━━  100%  15.8 MB   --
  Pulling                                     ━━━━━━━━━━━━━━━━━━━━  100%           3/3 layers

  Pulled ghcr.io/myorg/webapp:v2.1.0
  Digest: sha256:7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b
```

### Non-TTY fallback (piped / CI)

```text
  Pulling (3 layers)
    [1/3] sha256:a3ed95 config.json (1.5 KB)
    ✓ sha256:a3ed95 config.json (1.5 KB)
    [2/3] sha256:e1b2f3 app-binary.tar.gz (45.6 MB)
    ✓ sha256:e1b2f3 app-binary.tar.gz (45.6 MB)
    [3/3] sha256:c4d5e6 static-assets.tar.gz (15.8 MB)
    ✓ sha256:c4d5e6 static-assets.tar.gz (15.8 MB)
  Pulled ghcr.io/myorg/webapp:v2.1.0
```

**How to launch:**

```bash
# Push with live progress
oras push ghcr.io/myorg/webapp:v2.1.0 ./app-binary.tar.gz ./static-assets.tar.gz

# Pull with live progress
oras pull ghcr.io/myorg/webapp:v2.1.0

# Pipe-safe — auto-detects non-TTY and uses plain text
oras pull ghcr.io/myorg/webapp:v2.1.0 | tee pull.log
```

---

## Manifest Inspector
{: .text-yellow-300 }

Drill into any manifest with a menu-driven inspector. View the raw JSON, a structural layer tree, config blob contents, or trigger actions like pull/copy/delete.

### Inspector Menu

```text
  ── Manifest Inspector: ghcr.io/myorg/webapp:v2.1.0 ─────────────────

  Select an option:
  ❯ View Manifest JSON
    View Layer Tree
    View Config Blob
    Actions (Pull/Copy/Delete)
    Back to tag list
```

### Manifest JSON

```text
  ╭─ Manifest JSON ────────────────────────────────────────────────────╮
  │  {                                                                 │
  │    "schemaVersion": 2,                                             │
  │    "mediaType": "application/vnd.oci.image.manifest.v1+json",      │
  │    "config": {                                                     │
  │      "mediaType": "application/vnd.oci.image.config.v1+json",      │
  │      "digest": "sha256:a3ed95caeb02...",                           │
  │      "size": 1536                                                  │
  │    },                                                              │
  │    "layers": [                                                     │
  │      {                                                             │
  │        "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip", │
  │        "digest": "sha256:e1b2f3c4d5e6...",                         │
  │        "size": 47839885                                            │
  │      },                                                            │
  │      {                                                             │
  │        "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip", │
  │        "digest": "sha256:c4d5e6f7a8b9...",                         │
  │        "size": 16567910                                            │
  │      }                                                             │
  │    ]                                                               │
  │  }                                                                 │
  ╰────────────────────────────────────────────────────────────────────╯
```

### Layer Tree

```text
  ╭─ Layer Tree ───────────────────────────────────────────────────────╮
  │                                                                    │
  │  Manifest (application/vnd.oci.image.manifest.v1+json)             │
  │  ├── config (application/vnd.oci.image.config.v1+json)             │
  │  │   └── sha256:a3ed95caeb02... (1.5 KB)                          │
  │  ├── layers (2 total)                                              │
  │  │   ├── [0] application/vnd.oci.image.layer.v1.tar+gzip          │
  │  │   │   └── sha256:e1b2f3c4d5e6... (45.6 MB)                     │
  │  │   └── [1] application/vnd.oci.image.layer.v1.tar+gzip          │
  │  │       └── sha256:c4d5e6f7a8b9... (15.8 MB)                     │
  │  └── referrers (1 total)                                           │
  │      └── application/vnd.example.sbom.v1                           │
  │          └── sha256:sbom123abc... (4 KB)                           │
  │                                                                    │
  ╰────────────────────────────────────────────────────────────────────╯
```

**How to launch:**

```bash
# From the TUI browser — select a tag to open the inspector
oras tui --registry ghcr.io --repository myorg/webapp

# Or inspect a manifest directly from CLI
oras manifest fetch ghcr.io/myorg/webapp:v2.1.0
```

---

## Tag Management
{: .text-yellow-300 }

View tags in a styled table with digests, compressed sizes, and timestamps. Add or remove tags through the TUI or CLI.

### Tag Table (`--format text`)

```text
  ╭──────────────────┬──────────────────────┬──────────┬─────────────────────╮
  │ Tag              │ Digest               │ Size     │ Created             │
  ├──────────────────┼──────────────────────┼──────────┼─────────────────────┤
  │ latest           │ sha256:7a8b9c0d1e2f  │ 62.9 MB  │ 2026-03-06 14:22   │
  │ v2.1.0           │ sha256:7a8b9c0d1e2f  │ 62.9 MB  │ 2026-03-06 14:22   │
  │ v2.0.0           │ sha256:d4e5f6a7b8c9  │ 61.2 MB  │ 2026-02-28 09:15   │
  │ v1.5.3           │ sha256:1a2b3c4d5e6f  │ 58.7 MB  │ 2026-02-14 11:03   │
  │ v1.5.2           │ sha256:f0e1d2c3b4a5  │ 58.5 MB  │ 2026-02-01 16:47   │
  │ v1.0.0           │ sha256:9f8e7d6c5b4a  │ 52.1 MB  │ 2026-01-10 08:30   │
  │ sha-a1b2c3d      │ sha256:7a8b9c0d1e2f  │ 62.9 MB  │ 2026-03-06 14:22   │
  │ nightly-20260305 │ sha256:b2c3d4e5f6a7  │ 63.4 MB  │ 2026-03-05 02:00   │
  ╰──────────────────┴──────────────────────┴──────────┴─────────────────────╯
```

### Tag Table (`--format json`)

```json
[
  {"tag":"latest","digest":"sha256:7a8b9c0d1e2f...","size":65958707,"created":"2026-03-06T14:22:00Z"},
  {"tag":"v2.1.0","digest":"sha256:7a8b9c0d1e2f...","size":65958707,"created":"2026-03-06T14:22:00Z"}
]
```

### Adding Tags

```text
  ℹ Enter tags separated by spaces (e.g., v1.0 latest stable):
  Tags: stable production
  ℹ Tag command: oras tag ghcr.io/myorg/webapp:v2.1.0 stable production
  ✓ Tagged ghcr.io/myorg/webapp:v2.1.0
```

**How to launch:**

```bash
# List tags from CLI
oras repo tags ghcr.io/myorg/webapp

# Tag an artifact
oras tag ghcr.io/myorg/webapp:v2.1.0 stable production

# Or manage tags interactively from the TUI inspector
oras tui --registry ghcr.io --repository myorg/webapp
```

---

## Interactive Selection
{: .text-yellow-300 }

Multi-select prompts powered by `MultiSelectionPrompt` for batch operations — select multiple repositories, tags, or artifacts in one go.

### Multi-Select Tags for Batch Delete

```text
  Select tags to delete:
  (Press <space> to select, <enter> to accept)

    [×] nightly-20260305
    [×] sha-a1b2c3d
    [ ] latest
    [ ] v2.1.0
    [ ] v2.0.0
    [ ] v1.5.3
    [ ] v1.5.2
    [ ] v1.0.0
```

### Confirmation for Destructive Actions

```text
  ⚠ You are about to delete: ghcr.io/myorg/webapp:nightly-20260305
  ⚠ Digest: sha256:b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3
  ⚠ Size: 63.4 MB

  Are you sure you want to delete this manifest? [y/N] y
  ✓ Deleted ghcr.io/myorg/webapp:nightly-20260305
```

### Multi-Select Repositories for Batch Pull

```text
  Select repositories to pull:
  (Press <space> to select, <enter> to accept)

    [×] myorg/webapp:latest
    [×] myorg/api-service:v3.0.0
    [ ] myorg/ml-model:v1.2
    [×] team/frontend:stable
    [ ] team/backend:latest
    [ ] demo/hello-oras:latest
```

**How to launch:**

```bash
# Multi-select is available throughout the TUI
oras tui

# Batch operations from the CLI
oras tag ghcr.io/myorg/webapp:v2.1.0 stable production release-candidate
```

---

## Color Reference
{: .text-yellow-300 }

The TUI uses a deliberate color palette via Spectre.Console styles:

| Element | Color | Usage |
|:--------|:------|:------|
| Borders & headers | `Cyan1` | Dashboard panel, primary emphasis |
| Success indicators | `Green` | `✓` checkmarks, "logged in" status |
| Warnings | `Yellow` | `⚠` icons, destructive-action prompts |
| Errors | `Red` | Error messages, failed operations |
| Info messages | `Blue` | `ℹ` informational text, transfer speeds |
| Dimmed text | `Grey` | Digests, secondary details, help text |
| Prompt accent | `Green` | User input cursor, prompt styling |

---

## Requirements

The interactive TUI requires a terminal that supports:

- **ANSI escape codes** — virtually all modern terminals (Windows Terminal, iTerm2, GNOME Terminal, etc.)
- **Unicode** — for box-drawing characters and status icons
- **Interactive stdin** — TUI auto-disables when output is piped or redirected

When the environment is non-interactive (CI pipelines, redirected output), all commands gracefully fall back to plain text with no escape codes.

```bash
# Force non-interactive mode
oras pull ghcr.io/myorg/webapp:v2.1.0 --no-tty
```

---

{: .text-center .fs-3 }
Ready to try it? [Install the CLI](installation) and run `oras tui` to explore.
