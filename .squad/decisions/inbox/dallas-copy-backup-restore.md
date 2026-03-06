# Decision: Copy Enhancement + Backup/Restore Command Design

**Date:** 2026-03-06  
**Author:** Dallas (Core Dev)  
**Status:** Implemented

## Context

Three commands needed implementation: enhancing `copy`, and creating new `backup`/`restore` commands. RegistryService is still stubbed awaiting OrasProject.Oras v0.5.0 API integration.

## Decisions

### D1: Source Auth Options on Copy
Added `--from-username` and `--from-password` as standalone options on `copy` (not part of RemoteOptions) since they only apply to the source registry. The existing RemoteOptions (`--username`, `--password`) apply to the destination.

### D2: Shared Validation Helpers
`CopyCommand.ValidateReference()` and `BackupCommand.IsArchivePath()` are `internal static` methods reused across commands rather than duplicated. This keeps validation consistent and DRY.

### D3: Simulation Over NotImplementedException
Commands now simulate their workflow with `AnsiConsole.Status` + `Task.Delay` and return exit code 0 with a "simulated" status field. This is preferable to throwing `NotImplementedException` because:
- Allows testing the full command pipeline (argument parsing, validation, output formatting)
- Users can verify the CLI structure before library integration lands
- JSON output includes a `"status": "simulated"` marker so callers can detect stub behavior

### D4: Archive Detection Convention
Backup/restore detect archive mode via file extension: `.tar`, `.tar.gz`, `.tgz`. Non-archive paths are treated as OCI layout directories. Backup creates the `oci-layout` marker file as a placeholder.

### D5: Backup Output Requirement
`--output` on backup is marked `Required = true` at the option level — System.CommandLine enforces this before the handler runs.

## Impact
- **For Hicks:** New commands need test coverage (backup, restore, enhanced copy with --from-username/--from-password)
- **For Ripley:** Library integration TODOs are clearly marked with step-by-step comments in each command
