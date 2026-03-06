---
title: TUI Guide
layout: default
nav_order: 4
description: "Using the interactive Terminal UI for registry exploration"
---

# TUI Guide

> Using the interactive Terminal UI for registry exploration and artifact management

The ORAS .NET CLI v0.2.0 includes a powerful terminal user interface (TUI) with elegant visual design, context menus, in-memory caching, and fully interactive workflows for exploring container registries and managing artifacts.

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
- FigletText ASCII art "ORAS" header for visual impact
- Credential store status and statistics with ASCII-safe indicators (`[+]` logged in, `[ ]` not authenticated)
- Quick actions for all artifact operations
- In-memory caching with "(cached)" indicators for fast repeat operations

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

### Keyboard Navigation
- **Arrow keys**: Navigate lists and trees
- **Enter**: Select item / perform action
- **Esc**: Go back / cancel
- **Ctrl+C**: Exit application
- **F5**: Refresh current view

## ASCII-Safe Indicators

All status symbols are ASCII-safe for universal terminal compatibility — no Unicode symbols:

- `[+]` — Success, logged-in status
- `[i]` — Informational messages
- `[!]` — Warnings, destructive actions
- `[X]` — Errors

## Tips

- Use `--debug` flag for verbose logging: `oras --debug`
- Use `--no-tty` to force non-interactive mode
- The TUI respects your Docker credentials automatically

For more details on individual commands, see the [Command Reference](commands/).
