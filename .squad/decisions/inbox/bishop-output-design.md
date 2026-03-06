# Output System Design — Bishop

**Date:** 2026-03-06  
**Author:** Bishop (TUI Dev)  
**Status:** Implemented (Sprint 1)

## Decision

Implemented the output formatting abstraction (S1-03) and progress rendering system (S1-15) as the foundation for all CLI output, supporting both human-readable (text) and machine-readable (JSON) formats.

## Implementation Details

### IOutputFormatter Abstraction

Created a comprehensive interface supporting:
- **Status messages:** Success/info messages with color coding in TTY mode
- **Error messages:** Structured errors with optional recommendations (aligns with ADR-008)
- **Tables:** Repository lists, tag lists, etc. (styled in TTY, tab-separated in plain text)
- **Trees:** Manifest layer trees, referrer trees (Spectre.Console Tree widget with fallback)
- **JSON:** Syntax-highlighted in TTY via `JsonText`, plain in non-TTY
- **Descriptors:** Structured output for resolve, blob fetch --descriptor, etc.

### TextFormatter Implementation

- **TTY detection:** Uses `Spectre.Console.Profile.Capabilities.Ansi` to determine styling support
- **Markup escaping:** All user input properly escaped via `Markup.Escape()` to prevent injection
- **Tables:** Leverages Spectre.Console `Table` widget with automatic column sizing
- **Trees:** Builds `Tree` from `TreeNode` model with metadata support
- **JSON:** Uses `JsonText` for syntax highlighting when available

### JsonFormatter Implementation

- **Consistent structure:** All output as JSON objects with `status` field for machine parsing
- **camelCase properties:** Standard JSON convention via `JsonNamingPolicy.CamelCase`
- **One object per line:** Enables streaming processing and log aggregation
- **No interactivity:** `SupportsInteractivity = false` prevents progress bars in JSON mode

### ProgressRenderer System

- **Interactive mode:** Multi-task progress display using `AnsiConsole.Progress()` with:
  - Per-layer progress bars (digest, filename, size, percentage)
  - Custom `TransferSpeedColumn` for real-time speed display
  - Overall summary bar for total operation progress
  - Visual completion indicators (✓ checkmarks)
- **Non-interactive fallback:** Simple line-by-line output with layer counts
- **Callback architecture:** `IProgressCallback` interface with adapter pattern for library integration
- **Lifecycle management:** Implements `IDisposable` for proper cleanup

### Integration with FormatOptions

Updated Dallas's `FormatOptions` class to:
- Use System.CommandLine 2.x API (`AcceptOnlyFromAmong`, `DefaultValueFactory`)
- Add `CreateFormatter()` factory method that returns appropriate `IOutputFormatter`
- Support both `--format` (text|json) and `--pretty` options

## Rationale

1. **Separation of concerns:** Formatting logic is decoupled from command logic, making commands testable without worrying about rendering
2. **TTY-aware:** Automatically adapts to the output environment (terminal vs pipe vs redirect)
3. **Consistent UX:** All commands use the same output patterns, providing predictable user experience
4. **Machine-readable:** JSON mode enables scripting and CI/CD integration
5. **Testable:** `IAnsiConsole` injection allows unit testing of formatters without actual console
6. **Extensible:** Adding new output formats (e.g., template-based) only requires implementing `IOutputFormatter`

## Future Considerations

1. **Template support:** If Go template parity is needed later, add `TemplateFormatter` implementing `IOutputFormatter`
2. **Progress hooks:** Services will need to wire `ProgressRenderer` to `CopyGraphOptions.PreCopy`/`PostCopy` callbacks in the oras-dotnet library
3. **Error translation:** Commands should use `IOutputFormatter.WriteError()` for consistent error display (ties into ADR-008 structured errors)
4. **TUI integration:** Sprint 3 TUI work will reuse `ProgressRenderer` and `TreeNode` models for dashboard and browser views

## Impact

- Commands can focus on business logic, delegating all rendering to formatters
- Users get consistent, professional output across all commands
- CI/CD pipelines can reliably parse JSON output for automation
- Foundation laid for Sprint 3 interactive TUI features
