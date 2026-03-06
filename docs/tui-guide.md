# TUI Guide

> Using the interactive Terminal UI for registry exploration

The ORAS .NET CLI includes a powerful terminal user interface (TUI) for exploring container registries and managing artifacts interactively.

## Launching the TUI

```bash
# Launch TUI to main dashboard
oras tui

# Launch directly to registry browser
oras tui --registry ghcr.io

# Browse a specific repository
oras tui --registry ghcr.io --repository myorg/myrepo
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

### Keyboard Navigation
- **Arrow keys**: Navigate lists and trees
- **Enter**: Select item / perform action
- **Esc**: Go back / cancel
- **Ctrl+C**: Exit application
- **F5**: Refresh current view

## Tips

- Use `--debug` flag for verbose logging: `oras tui --debug`
- Use `--no-tty` to force non-interactive mode
- The TUI respects your Docker credentials automatically

For more details on individual commands, see the [Command Reference](commands/).
