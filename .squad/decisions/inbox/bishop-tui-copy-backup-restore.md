# DEC-TUI-002 — Interactive Copy, Backup, Restore Workflows

**Date:** 2026-03-06
**Author:** Bishop (TUI Dev)
**Status:** Implemented

## Decision

Added interactive TUI workflows for Copy, Backup, and Restore in Dashboard.cs. Copy replaces the former CLI-only placeholder. Backup and Restore are new dashboard actions.

## Rationale

- Copy is a common operation that benefits from guided prompts (source, destination, referrers toggle) rather than requiring users to remember CLI syntax.
- Backup/Restore are new first-class operations that pair naturally: backup exports an artifact to local storage, restore pushes it back to a registry.
- All three use simulated progress (`Task.Delay`) until the underlying services are wired. This lets the UX be validated independently of service implementation.

## Menu Order

Interactive actions are grouped above CLI-only hints:
1. Browse Registry
2. Browse Repository Tags
3. Login
4. Copy Artifact (interactive)
5. Backup Artifact (interactive, NEW)
6. Restore Artifact (interactive, NEW)
7. Push Artifact (CLI-only hint)
8. Pull Artifact (CLI-only hint)
9. Tag Artifact (CLI-only hint)
10. Quit

## Integration Points

- `HandleCopyArtifactAsync` — ready for `ICopyService` integration
- `HandleBackupArtifactAsync` — ready for `IBackupService` integration (export to directory or tar.gz)
- `HandleRestoreArtifactAsync` — ready for `IRestoreService` integration (import from directory or tar.gz)
- Restore validates filesystem paths before proceeding; Backup defaults output to `./backup`

## Impact

- Dashboard.cs only. No changes to commands, services, or other TUI components.
- Build: 0 errors, warnings unchanged.
