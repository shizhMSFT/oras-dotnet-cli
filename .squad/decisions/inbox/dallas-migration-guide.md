# Decision: Migration Guide Accuracy Policy

**Author:** Dallas (Core Developer)
**Date:** 2026-03-06
**Context:** Migration guide creation

## Decision

The migration guide (`docs/migration.md`) reports command implementation status based on direct source code inspection — not assumptions or roadmap intent. Each command was classified by whether it throws `NotImplementedException` (stub), has partial logic (partial), or is fully functional (full).

## Rationale

Migration guides that overstate readiness erode trust. Users switching from a production Go CLI need honest status reporting. The guide should be updated whenever a stubbed command gets its real implementation.

## Impact

- Any PR that implements a previously-stubbed command should also update the migration guide's command comparison table.
- The "Known Limitations" section should shrink as commands move from stub → full.
