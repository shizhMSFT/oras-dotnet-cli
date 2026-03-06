---
title: TUI Guide
layout: default
nav_order: 4
description: "Using the interactive Terminal UI for registry exploration"
---

# TUI Guide

> Using the interactive Terminal UI for registry exploration and artifact management

The ORAS .NET CLI v0.3.0 includes a powerful terminal user interface (TUI) with elegant visual design, context menus, in-memory caching, and fully interactive workflows for exploring container registries and managing artifacts.

## Launching the TUI

```bash
# Launch TUI to main dashboard (just run oras with no arguments)
oras

# Launch directly to registry browser
oras --registry ghcr.io

# Browse a specific repository
oras --registry ghcr.io --repository myorg/myrepo
```

## Features

### Dashboard View
- Hand-crafted Unicode block art "ORAS" banner with brand colors (O=#D04485 pink, R=#5EBAB4 teal, A=#FCFCFD white, S=#CCF575 lime)
- Subtitle: "OCI Registry As Storage | v{version} • Interactive Terminal UI"
- Credential store status with Unicode indicators (`●` Authenticated, `○` No credentials) for all registries (auths, credHelpers, and credsStore)
- Artifact operations accessible via a dedicated "Artifacts" sub-menu
- In-memory caching with "(cached)" indicators for fast repeat operations
- UTF-8 output encoding ensures correct rendering on all platforms

### Registry Browser
- Hierarchical repository navigation with live search
- Tag and manifest exploration
- **Context menu** on repository selection:
  - Browse Tags
  - Copy entire repository
  - Backup repository
- Artifact metadata inspection
- **Catalog-less registries**: When a registry doesn't support the catalog API (e.g., ghcr.io), the browser shows a clear message. Use "Enter repository name..." to type a repository path directly.

### Tag Context Menu
- Selecting a tag shows:
  - Inspect Manifest
  - Pull to directory
  - Copy to...
  - Backup to local
  - Tag with...
  - Delete
  - ─── (non-selectable separator)
  - Back

### Direct Repository Browse
- Jump straight to a repository's tags without browsing the catalog
- Supports registries that don't have a catalog API
- Select "Browse Repository Tags" on the dashboard or use `oras --repository <name>`

### Copy Artifact
- Copy OCI artifacts between registries with live progress tracking and visual progress bars
- Supports separate source and destination credentials
- Optional: Include referrers (signatures, attestations, SBOMs) with `--recursive`
- Fully interactive TUI workflow
- Available from TUI dashboard or CLI: `oras copy <source> <destination>`

### Backup Artifact
- Save registry artifacts to a local OCI layout directory or tar archive with live progress
- Perfect for disaster recovery, air-gapped environments, and offline workflows
- Supports `--recursive` to backup entire artifact graphs
- Fully interactive TUI workflow with summary panel
- Available from TUI dashboard or CLI: `oras backup <reference> --output <path>`

### Restore Artifact
- Push artifacts from a local backup to any OCI-compliant registry with live progress tracking
- Supports restoring from both OCI layout directories and tar archives
- Can restore to a different registry or repository than the original
- Fully interactive TUI workflow
- Available from TUI dashboard or CLI: `oras restore <path> <destination>`

### In-Memory Caching
- Frequently accessed registries, repositories, and manifests are cached with TTL
- "(cached)" indicator shown in lists for cached entries
- "Refresh" options available in context menus to invalidate cache
- Dramatically faster navigation on repeat visits

### Artifacts Sub-Menu
- Artifact operations are grouped under a dedicated "Artifacts" menu entry:
  - Push Artifact
  - Pull Artifact
  - Copy Artifact
  - Tag Artifact
  - Backup Artifact
  - Restore Artifact
  - ─── (non-selectable separator)
  - Back

### Cyclic Navigation
- All menus wrap around: pressing ↓ from the last item moves to the first item, and ↑ from the first item moves to the last
- Non-selectable separators (`───`) visually group menu items (e.g., before Quit or Back) using `AddChoiceGroup`

### Keyboard Navigation
- **Arrow keys**: Navigate lists and trees (with cyclic wrapping)
- **Enter**: Select item / perform action
- **Esc**: Go back / cancel
- **Ctrl+C**: Exit application
- **F5**: Refresh current view

## Unicode Indicators

Status symbols use Unicode characters for rich terminal rendering:

- `✔` (green) — Success, completed operations
- `✗` (red) — Errors, failed operations
- `ℹ` (cyan) — Informational messages
- `⚠` (yellow) — Warnings, destructive actions

## Tips

- Use `--debug` flag for verbose logging: `oras --debug`
- Use `--no-tty` to force non-interactive mode
- The TUI respects your Docker credentials automatically

For more details on individual commands, see the [Command Reference](commands/).
