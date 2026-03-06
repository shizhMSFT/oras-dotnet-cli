# Bishop TUI Refactor — ArtifactActions Extraction

**Date:** 2026-03-07  
**Owner:** Bishop  
**Status:** Accepted

## Context
Dashboard, RegistryBrowser, and ManifestInspector each contained near-identical action handlers for pull, copy, backup, restore, tag, and delete operations. The duplicated progress scaffolding made maintenance risky and blocked consistent UX updates.

## Decision
Introduce a shared `ArtifactActions` helper in `src/Oras.Cli/Tui/` to centralize the prompt/validate/progress/success/error flow, while keeping each screen responsible for its own parameter prompts. Add `PromptHelper.PressEnterToContinue()` and merge selection prompts into a single method with an `enableSearch` flag to reduce boilerplate.

## Consequences
- TUI action handlers are now thin orchestration layers with shared progress UX.
- Updates to action flows can be made once without triplicate edits.
- Minor internal API additions (PromptHelper, ArtifactActions) establish a common UX foundation for future TUI work.
