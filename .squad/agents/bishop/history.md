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

<!-- Append new learnings below. Each entry is something lasting about the project. -->
