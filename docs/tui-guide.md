---
title: TUI Guide
layout: default
nav_order: 4
description: "Using the interactive Terminal UI for registry exploration"
---

# TUI Guide

> Using the interactive Terminal UI for registry exploration

The ORAS .NET CLI includes a powerful terminal user interface (TUI) for exploring container registries and managing artifacts interactively.

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
- Credential store status and statistics
- Recent operations history
- Quick actions for common tasks

### Registry Browser
- Hierarchical repository navigation
- Tag and manifest exploration
- Copy reference, pull, attach operations
- Artifact metadata inspection
- **Catalog-less registries**: When a registry doesn't support the catalog API (e.g., ghcr.io), the browser shows a clear message. Use "Enter repository name..." to type a repository path directly.

### Direct Repository Browse
- Jump straight to a repository's tags without browsing the catalog
- Supports registries that don't have a catalog API
- Select "Browse Repository Tags" on the dashboard or use `oras --repository <name>`

### Copy Artifact
- Copy OCI artifacts between registries with live progress tracking
- Supports separate source and destination credentials
- Optional: Include referrers (signatures, attestations, SBOMs) with `--recursive`
- Available from TUI dashboard or CLI: `oras copy <source> <destination>`

### Backup Artifact
- Save registry artifacts to a local OCI layout directory or tar archive
- Perfect for disaster recovery, air-gapped environments, and offline workflows
- Supports `--recursive` to backup entire artifact graphs
- Available from TUI dashboard or CLI: `oras backup <reference> --output <path>`

### Restore Artifact
- Push artifacts from a local backup to any OCI-compliant registry
- Supports restoring from both OCI layout directories and tar archives
- Can restore to a different registry or repository than the original
- Available from TUI dashboard or CLI: `oras restore <path> <destination>`

### Keyboard Navigation
- **Arrow keys**: Navigate lists and trees
- **Enter**: Select item / perform action
- **Esc**: Go back / cancel
- **Ctrl+C**: Exit application
- **F5**: Refresh current view

## Tips

- Use `--debug` flag for verbose logging: `oras --debug`
- Use `--no-tty` to force non-interactive mode
- The TUI respects your Docker credentials automatically

For more details on individual commands, see the [Command Reference](commands/).
