# Decision: Sprint 3 TUI Implementation

**Date:** 2026-03-06
**Author:** Bishop (TUI Dev)
**Status:** Implemented

## Context

Sprint 3 requirements called for a complete interactive TUI mode to differentiate the .NET CLI from the Go CLI, which only shows help text when invoked without arguments. The TUI needed to provide:
- Dashboard with registry overview and quick actions
- Interactive registry browser with repo/tag exploration
- Manifest inspector with JSON, tree, and action views
- Enhanced progress visualization for push/pull operations
- Consistent prompt UX across all interactions

## Decisions Made

### 1. TUI Architecture: Four Core Components

**Decision:** Implement TUI as four separate, focused classes in `src/Oras.Cli/Tui/`:
- `PromptHelper` — Reusable prompt utilities
- `Dashboard` — Main entry point
- `RegistryBrowser` — Browse flow
- `ManifestInspector` — Manifest viewer

**Rationale:**
- Clear separation of concerns
- Reusable prompt patterns via PromptHelper
- Each component has a single, well-defined responsibility
- Easy to test and maintain independently

### 2. TTY Detection in Program.cs

**Decision:** Check `Dashboard.ShouldLaunchTui(args)` before creating root command. Criteria:
- No command-line arguments
- stdout not redirected (`!Console.IsOutputRedirected`)
- stderr not redirected (`!Console.IsErrorRedirected`)

**Rationale:**
- Matches Go CLI behavior (but launches TUI instead of showing help)
- Respects output redirection for scripting/CI compatibility
- Clean integration point without modifying System.CommandLine parsing logic

### 3. Mock Data Strategy for S3 Delivery

**Decision:** Implement complete TUI flow with mock data. Real oras-dotnet integration marked with TODO comments.

**Rationale:**
- Allows full UX validation and testing without blocking on library API integration
- Services (RegistryService, etc.) already have NotImplementedException stubs
- Mock data demonstrates correct UX patterns for future integration
- Shiwei can validate TUI experience immediately

**Example mock locations:**
- `FetchRepositoriesAsync()` — returns `["example/app", "example/service", ...]`
- `FetchTagsAsync()` — returns `["latest", "v1.0", "v1.1", ...]`
- `FetchManifestAsync()` — returns structured ManifestData with sample layers
- `VerifyRegistryConnectionAsync()` — simulates 500ms connection check

### 4. Spectre.Console API Workarounds

**Decision:** Use `Markup` with `Escape()` for JSON display instead of `JsonText` (which is not available in Spectre.Console 0.50.0).

**Rationale:**
- JsonText class does not exist in the installed version
- Using `Panel(new Markup($"[dim]{Markup.Escape(json)}[/]"))` provides readable JSON display
- Maintains consistent Panel-based layout throughout TUI
- Future upgrade can replace with JsonText if it becomes available

**Other API adaptations:**
- `Rule.Justification` (not `Alignment`)
- `Style(foreground: Color.Cyan1)` constructor syntax
- `SelectionPrompt.EnableSearch()` for searchable lists

### 5. Browser Actions: Command-Line Fallback

**Decision:** Browser actions (Pull, Copy, Delete) show command-line equivalents instead of executing directly.

**Rationale:**
- Service implementations (PullService, CopyService) are not fully integrated with oras-dotnet yet
- Showing command syntax provides immediate value and educational benefit
- Users can copy/paste commands for execution
- Full integration can be added in future sprint when services are functional

**Example output:**
```
Pull command: oras pull localhost:5000/example/app:latest
Use the command line to pull artifacts.
```

### 6. Progressive Disclosure in Manifest Inspector

**Decision:** Use menu-driven navigation instead of showing all views at once:
1. Select action: View JSON / View Tree / View Config / Actions / Back
2. Show selected view in full screen
3. Return to menu after each view

**Rationale:**
- Keeps screen uncluttered
- Users can focus on one aspect at a time
- Works well in smaller terminal windows
- Follows common TUI navigation patterns (e.g., htop, lazygit)

### 7. Reusable PromptHelper Patterns

**Decision:** Centralize all prompts in `PromptHelper` static class with consistent signatures:
- `PromptText()` — text input
- `PromptSecret()` — masked password input
- `PromptSelection()` — single selection from list
- `PromptSelectionWithSearch()` — selection with search enabled
- `PromptMultiSelection()` — multi-select for batch operations
- `PromptConfirmation()` — yes/no confirmation
- `ShowError/Success/Info/Warning()` — status messages with icons

**Rationale:**
- Consistent UX across all TUI components
- Easy to extend or modify prompts globally
- Single source of truth for prompt styling
- Reduces boilerplate in Dashboard, Browser, Inspector classes

## Implementation Notes

- Build passes with 0 errors (only pre-existing warnings)
- All TUI components follow file-scoped namespace pattern
- Uses existing ICredentialService and DockerConfigStore
- ProgressRenderer (S3-07) was already implemented in Sprint 1 with enhanced features
- No breaking changes to existing command infrastructure

## Future Work

- Replace mock data with real oras-dotnet library calls (RegistryService.CreateRegistryAsync, etc.)
- Integrate command services for browser actions (Pull, Copy, Tag, Delete)
- Add unit tests for TUI components using Spectre.Console test infrastructure (IAnsiConsole mock)
- Add keyboard shortcuts (e.g., Ctrl+C for cancel, / for search activation)
- Consider adding TUI-specific configuration (theme, default registry, etc.)

## Open Questions

None. Implementation is complete per S3-01 through S3-08 requirements, pending integration with real library APIs.
