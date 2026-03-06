# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

### 2026-03-06 — Sprint 1 TUI Infrastructure Implementation (S1-03, S1-15)

**Implemented IOutputFormatter abstraction (S1-03):**
- Created `IOutputFormatter` interface in `src/Oras.Cli/Output/` with methods for tables, trees, JSON, descriptors, status messages, and errors
- `TextFormatter`: Uses Spectre.Console for TTY (styled tables via `Table`, syntax-highlighted JSON via `JsonText`, tree views via `Tree`), with plain text fallback for non-TTY environments
- `JsonFormatter`: Structured JSON output for `--format json` (machine-readable, camelCase property names, one JSON object per line)
- Integrated with Dallas's existing `FormatOptions` class, updated to use System.CommandLine 2.x API and added `CreateFormatter` factory method

**Implemented ProgressRenderer (S1-15):**
- Created `ProgressRenderer` using `AnsiConsole.Progress()` for push/pull operations with per-layer progress bars and overall summary
- Designed with hookable callbacks: `OnLayerStart`, `OnLayerProgress`, `OnLayerComplete`, `OnOverallProgress` for integration with oras-dotnet library callbacks
- Plain text fallback for non-TTY: simple `[layer N/M] downloading...` style output with checkmarks on completion
- Custom `TransferSpeedColumn` for showing real-time transfer speeds in human-readable format (B/s, KB/s, MB/s, GB/s)
- `ProgressCallbackAdapter` provides clean adapter pattern for connecting to library copy operations via `CopyGraphOptions.PreCopy`/`PostCopy`

**System.CommandLine 2.x API patterns learned:**
- Option constructor syntax: `new Option<T>("--name", "-alias")` (aliases as separate params, not named parameter)
- Setting defaults: `DefaultValueFactory = _ => value` (not `getDefaultValue` constructor param)
- Restricting values: `option.AcceptOnlyFromAmong(values)` (not `FromAmong` property)
- Adding options to commands: `command.Options.Add(option)` (Options is now IList, not IReadOnlyList)
- Global options: Set `Recursive = true` property to apply option to all subcommands

**Code conventions applied:**
- File-scoped namespaces (`namespace Oras.Output;`)
- Proper nullable annotations throughout
- PascalCase for public members, interfaces prefixed with `I`
- 4-space indentation, UTF-8 encoding
- Comments only where needed for clarity (no over-commenting)

**Files created:**
- `src/Oras.Cli/Output/IOutputFormatter.cs` - Interface and TreeNode model
- `src/Oras.Cli/Output/TextFormatter.cs` - Spectre.Console-based text formatter with TTY detection
- `src/Oras.Cli/Output/JsonFormatter.cs` - JSON formatter for machine-readable output
- `src/Oras.Cli/Output/ProgressRenderer.cs` - Progress rendering with callbacks and custom columns

**Files modified:**
- `src/Oras.Cli/Options/FormatOptions.cs` - Updated to System.CommandLine 2.x API and added CreateFormatter method

**Integration points for future work:**
- Commands should call `FormatOptions.CreateFormatter(format)` to get the appropriate formatter
- Services implementing push/pull should create a `ProgressRenderer` and use `ProgressCallbackAdapter` to hook into library callbacks
- The `IOutputFormatter.SupportsInteractivity` property can be used to determine if interactive prompts/progress bars should be shown

### 2026-03-06 — System.CommandLine 2.0.3 Stable Migration Patterns (from Dallas)

**Key API Changes to Know:**
1. **Option Construction:** Use `new Option<T>("--name", "-alias")` with aliases as separate constructor parameters (not named parameter)
2. **Default Values:** Use `DefaultValueFactory = _ => value` in object initializer (not SetDefaultValue method)
3. **Value Restriction:** Use `option.AcceptOnlyFromAmong(values)` (not FromAmong property)
4. **Command Handlers:** Use `SetAction(async (parseResult, cancellationToken) => { ... })` (not SetHandler with InvocationContext)
5. **Value Retrieval:** Use unified `parseResult.GetValue(option)` for both options and arguments
6. **Invocation:** Use `rootCommand.Parse(args).InvokeAsync()` (not direct InvokeAsync)

**Testing Impact:**
- System.CommandLine.IO.TestConsole was removed in 2.0.3
- Alternative: Use Console.SetOut/SetError with StringWriter for output capture
- This is more portable and doesn't depend on internal testing APIs

**Impact on ProgressRenderer:**
- ProgressRenderer.cs uses IRenderable from Spectre.Console.Rendering
- Make sure to include `using Spectre.Console.Rendering;` when rendering with ProgressRenderer
- The stable API is now locked; no breaking changes expected in future 2.x releases

### 2026-03-06 — Sprint 3 TUI Implementation (S3-01 through S3-08)

**Implemented complete interactive TUI mode:**
- Created TUI infrastructure in `src/Oras.Cli/Tui/` directory with four core components:
  - `PromptHelper.cs` — Reusable prompt utilities for consistent UX (selections, confirmations, multi-select, secrets)
  - `Dashboard.cs` — Main TUI entry point with registry overview, quick actions menu, login flow
  - `RegistryBrowser.cs` — Interactive registry browsing (connect, repo list, tag list)
  - `ManifestInspector.cs` — Manifest viewer with JSON display, layer tree, config blob preview, and actions menu

**S3-01 TUI Dashboard:**
- Detects TTY environment (no args + not redirected) to launch interactive mode
- Shows connected registries from Docker config store with auth status
- Quick actions menu: Browse, Login, Push, Pull, Copy, Tag, Quit
- Integrated with Program.cs to route no-args invocation to dashboard

**S3-02 Registry Connection:**
- Select from stored credentials or enter new registry URL
- Auth prompt with username/password for registries without stored credentials
- Validates credentials and stores in Docker config store
- Connection verification flow with status feedback

**S3-03 Repository List:**
- Paginated, searchable repository list using `SelectionPrompt` with search enabled
- Shows total repository count
- Mock data implementation (TODO: integrate with IRegistry.ListRepositoriesAsync())
- Seamless navigation back to main menu

**S3-04 Tag List:**
- Tag list for selected repository with search/filter support
- Sortable display (mock data, ready for IRepository.ListTagsAsync())
- Shows tag count
- Navigation between repositories and manifest inspector

**S3-05 Manifest Inspector:**
- Menu-driven inspector: View JSON, Layer Tree, Config Blob, Actions
- JSON display using Spectre.Console Panel with Markup (JsonText not available in v0.50.0)
- Layer tree view using Spectre.Console Tree widget with config, layers, and referrers nodes
- Config blob preview with syntax formatting
- Mock manifest data (TODO: integrate with IManifestStore.FetchAsync())

**S3-06 Browser Actions:**
- Contextual actions from inspector: Pull, Copy, Tag, Delete
- Multi-tag input for batch tagging
- Confirmation prompts for destructive operations (delete) with manifest details
- Integration points for command services (shows command line equivalents for now)

**S3-07 Enhanced Progress (existing):**
- ProgressRenderer already implemented with per-layer bars, transfer speed column, ETA support
- Uses AnsiConsole.Progress() with custom TransferSpeedColumn
- Real-time updates via Live rendering
- Plain text fallback for non-TTY

**S3-08 Selection Prompts:**
- Multi-selection support via MultiSelectionPrompt for batch operations
- Confirmation dialogs for destructive actions with PromptConfirmation
- Search/filter in all lists using EnableSearch() on SelectionPrompt
- Consistent prompt UX across all TUI components via PromptHelper

**Spectre.Console 0.50.0 API patterns learned:**
- `Rule.Justification` (not Alignment) for rule alignment
- `Style(foreground: Color.X)` constructor syntax for colored styles
- `Color.Cyan1`, `Color.Yellow` etc. for predefined colors (not Color.Cyan enum)
- `JsonText` not available in v0.50.0; use Markup with Escape for JSON display as workaround
- `SelectionPrompt.EnableSearch()` for searchable lists
- `MultiSelectionPrompt` for batch selections
- `Panel`, `Tree`, `Table` widgets for structured TUI layouts

**Integration with existing infrastructure:**
- Uses ICredentialService for auth operations
- Uses DockerConfigStore to list connected registries
- Dashboard.ShouldLaunchTui() checks TTY environment (no redirection)
- Program.cs routes no-args + TTY to dashboard before creating root command
- All TUI components designed to integrate with real service implementations (currently use mock data)

**Files created:**
- `src/Oras.Cli/Tui/PromptHelper.cs` — Reusable prompt utilities
- `src/Oras.Cli/Tui/Dashboard.cs` — Main TUI dashboard with quick actions
- `src/Oras.Cli/Tui/RegistryBrowser.cs` — Interactive registry/repo/tag browser
- `src/Oras.Cli/Tui/ManifestInspector.cs` — Manifest viewer with tree/JSON/actions

**Files modified:**
- `src/Oras.Cli/Program.cs` — Added TUI detection and dashboard launch logic

**Next steps for TUI completion:**
- Replace mock data with real OrasProject.Oras library calls once API is properly integrated
- Integrate command services (PullService, CopyService, TagService) for browser actions
- Add unit tests for TUI components using Spectre.Console test infrastructure

### 2026-03-06 — TUI Showcase Documentation Page

**Created `docs/tui-showcase.md`:**
- Visual showcase page for the GitHub Pages docs site (just-the-docs theme, dark mode, nav_order: 6)
- Six feature sections with realistic terminal output blocks: Dashboard, Registry Browser, Push/Pull Progress, Manifest Inspector, Tag Management, Interactive Selection
- Terminal output modeled directly from actual Spectre.Console widget rendering in `src/Oras.Cli/Tui/` and `src/Oras.Cli/Output/ProgressRenderer.cs`
- Uses Unicode box-drawing characters (╔═╗, ╭─╮, ├──, └──) to reproduce Spectre.Console `Panel`, `Table`, `Tree` widgets
- Includes color reference table mapping Spectre.Console styles (Cyan1, Green, Yellow, Red, Blue, Grey) to their TUI roles
- Non-TTY fallback output shown alongside interactive versions for progress bars
- Added link to TUI Showcase in `docs/index.md` Documentation section table

**Documentation conventions learned:**
- just-the-docs uses `nav_order` for sidebar ordering; existing pages use 1–5, new pages should follow sequentially
- `{: .text-yellow-300 }` Kramdown attribute for section header styling in dark mode
- `text` code fence (not `ansi`) is the reliable choice for terminal output in Jekyll/GitHub Pages — `ansi` fences have no standard rendering support

### Catalog Fallback — Manual Repository Entry for Non-Catalog Registries

**Problem:** Registries like ghcr.io do not support the `/v2/_catalog` API. The TUI browse flow was a dead-end when catalog failed.

**Design decisions:**
- `FetchRepositoriesAsync` uses null vs empty list semantics: null = catalog unsupported, empty = no repos. This distinction drives different user-facing messages.
- "Enter repository name..." is always available in the repo selection list — even when catalog succeeds — so users can jump to any repo they know.
- Unexpected fetch errors are treated as "catalog unavailable" (return null) rather than hard failures, so the user still has a path forward.
- `BrowseTagsAsync` was made public so Dashboard can invoke it directly for the "Browse Repository Tags" shortcut.
- Dashboard's new "Browse Repository Tags" action parses `registry/namespace/repo` by splitting on the first `/`, keeping parsing simple and consistent.

**Patterns reinforced:**
- Always use `Markup.Escape()` for user-provided text in Spectre.Console markup strings.
- Use `PromptHelper` methods for all interactive prompts — never raw `AnsiConsole.Ask` calls.
- `const string` for magic option labels like "Back to main menu" to avoid typo bugs.

**Files modified:**
- `src/Oras.Cli/Tui/RegistryBrowser.cs` — `BrowseRepositoriesAsync`, `FetchRepositoriesAsync`, `BrowseTagsAsync` (now public)
- `src/Oras.Cli/Tui/Dashboard.cs` — Added "Browse Repository Tags" action and `HandleBrowseRepositoryTagsAsync`

### 2026-03-06 — Sprint Wave Release: v0.1.2 (Catalog-Fallback Release)

**Release milestone:** v0.1.2 shipped with catalog-fallback feature.

**Files modified in this wave:**
- `src/Oras.Cli/Tui/RegistryBrowser.cs` — Finalized catalog fallback and manual entry logic
- `src/Oras.Cli/Tui/Dashboard.cs` — Browse Repository Tags action deployed
- Documentation updated by Vasquez (DevOps): tui-showcase.md, tui-guide.md, installation.md, index.md

**Key decision documented:** DEC-TUI-001 — Catalog API Fallback and DEC-DOC-001 — Terminal Output Blocks Use `text` Fences.

**Build status:** 0 errors, 0 warnings. Release published with 6 binaries.

### Interactive Copy, Backup, and Restore TUI Workflows

**Replaced Copy placeholder with interactive workflow:**
- Prompts for source reference, destination reference, and referrers inclusion
- Progress simulation using `AnsiConsole.Progress()` with four stages: resolve manifest, copy layers, copy manifest, copy referrers (optional)
- Uses `Markup.Escape()` on all user input before rendering in markup strings

**Added Backup Artifact workflow:**
- Prompts for source reference, output path (default `./backup`), and referrers inclusion
- Progress stages: fetch manifest, download layers, write output, download referrers (optional)
- Shows summary panel (Table widget) after completion with reference, layer count, estimated size, and output path

**Added Restore Artifact workflow:**
- Prompts for backup path with filesystem validation (`Directory.Exists` / `File.Exists`)
- Prompts for destination reference
- Progress stages: read backup, upload layers, push manifest
- Validates path exists before proceeding — shows error with recommendation if not found

**Dashboard menu reordered:**
- Interactive actions (Copy, Backup, Restore) placed above CLI-only hints (Push, Pull, Tag)
- Menu order: Browse Registry, Browse Repository Tags, Login, Copy, Backup, Restore, Push, Pull, Tag, Quit

**Patterns reinforced:**
- All handler methods follow try/catch with `OperationCanceledException` handled separately from general exceptions
- All workflows end with "Press Enter to continue..." before returning to dashboard loop
- `AnsiConsole.Progress()` with `.AutoClear(false).HideCompleted(false)` for persistent progress display
- Simulated operations use `Task.Delay` as placeholder until real service integration

<!-- Append new learnings below. Each entry is something lasting about the project. -->
