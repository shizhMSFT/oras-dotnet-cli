# Sprint 1 TUI Infrastructure — Implementation Summary

**Implementer:** Bishop (TUI Dev)  
**Date:** 2026-03-06  
**Sprint Items:** S1-03 (IOutputFormatter), S1-15 (ProgressRenderer)  
**Status:** ✅ Complete

---

## Deliverables

### S1-03: IOutputFormatter Abstraction

**Location:** `src/Oras.Cli/Output/`

#### Files Created:
1. **IOutputFormatter.cs** (1,738 bytes)
   - Interface defining output methods: status, error, table, tree, JSON, descriptor
   - `TreeNode` class for hierarchical output structures
   - `SupportsInteractivity` property for detecting TTY capabilities

2. **TextFormatter.cs** (5,236 bytes)
   - Uses Spectre.Console for styled terminal output
   - Automatic TTY detection via `IAnsiConsole.Profile.Capabilities`
   - Styled tables via `Table` widget
   - Syntax-highlighted JSON via `JsonText`
   - Tree rendering via `Tree` widget
   - Plain text fallback for non-TTY (pipes, redirects)

3. **JsonFormatter.cs** (2,541 bytes)
   - Machine-readable JSON output for `--format json`
   - One JSON object per line for streaming processing
   - camelCase property naming (standard JSON convention)
   - Structured output: `{"status":"success","message":"..."}`
   - No interactive features (`SupportsInteractivity = false`)

4. **README.md** (6,172 bytes)
   - Usage guide with code examples
   - Integration patterns for commands and services
   - TTY detection and JSON mode documentation
   - Best practices for output formatting

#### Files Modified:
1. **Options/FormatOptions.cs**
   - Updated to System.CommandLine 2.x API syntax
   - Added `CreateFormatter()` factory method
   - Returns appropriate `IOutputFormatter` based on `--format` flag

---

### S1-15: ProgressRenderer

**Location:** `src/Oras.Cli/Output/ProgressRenderer.cs` (7,502 bytes)

#### Features:
- **Interactive progress display** using `AnsiConsole.Progress()`
  - Per-layer progress bars with digest, filename, size, percentage
  - Custom `TransferSpeedColumn` for real-time transfer speed (B/s → GB/s)
  - Overall summary bar showing total operation progress
  - Visual completion indicators (✓ checkmarks)
  
- **Non-interactive fallback** for pipes/redirects
  - Simple line-by-line output: `[layer N/M] downloading... ✓`
  - No progress bars, just status messages

- **Callback architecture**
  - `OnLayerStart(digest, filename, size)` — Called when layer transfer begins
  - `OnLayerProgress(digest, bytesTransferred, totalBytes)` — Updates progress bar
  - `OnLayerComplete(digest, filename, size)` — Marks layer as complete
  - `OnOverallProgress(completed, total)` — Updates overall progress

- **Integration helpers**
  - `IProgressCallback` interface for library integration
  - `ProgressCallbackAdapter` connects to oras-dotnet's `CopyGraphOptions.PreCopy`/`PostCopy`
  - Implements `IDisposable` for proper cleanup

- **Human-readable formatting**
  - `FormatSize()` converts bytes to KB/MB/GB/TB
  - `FormatSpeed()` converts bytes/sec to appropriate units

---

## Integration Points

### For Command Implementations (Dallas):
```csharp
// Get the formatter based on --format flag
IOutputFormatter formatter = FormatOptions.CreateFormatter(format);

// Write status
formatter.WriteStatus("Pulled localhost:5000/hello:latest");

// Write errors with recommendations
formatter.WriteError("Failed to connect", "Check network and try again");

// Write tables (repo ls, repo tags)
formatter.WriteTable(headers, rows);

// Write trees (discover command)
formatter.WriteTree(manifestTree);
```

### For Service Layer (Dallas):
```csharp
// Create progress renderer for push/pull
using var progress = new ProgressRenderer();
progress.Start("Pushing artifact", totalLayers: 3);

// Hook into library callbacks
var copyOptions = new CopyGraphOptions
{
    PreCopy = (ctx, desc) => {
        progress.OnLayerStart(desc.Digest, filename, desc.Size);
        return Task.CompletedTask;
    },
    PostCopy = (ctx, desc) => {
        progress.OnLayerComplete(desc.Digest, filename, desc.Size);
        return Task.CompletedTask;
    }
};
```

---

## System.CommandLine 2.x API Patterns

Key differences from older versions (documented for team):

| Old API | New API (2.x) |
|---------|---------------|
| `new Option<T>(name: "--foo", aliases: ["-f"])` | `new Option<T>("--foo", "-f")` |
| `getDefaultValue: () => value` | `DefaultValueFactory = _ => value` |
| `{ FromAmong = [...] }` | `option.AcceptOnlyFromAmong(...)` |
| `command.AddOption(option)` | `command.Options.Add(option)` |

---

## Testing Notes

### What Works:
- All formatters compile successfully
- API follows System.CommandLine 2.x patterns
- Code adheres to project .editorconfig (file-scoped namespaces, nullable annotations, PascalCase)
- Integration with Spectre.Console is clean and testable (IAnsiConsole injection)

### Known Issues in Project (Not Bishop's Code):
- Other files (Dallas's Options, Services) have System.CommandLine API mismatches
- Missing `CredentialService` implementation (expected, S1-05 not complete)
- `RegistryOptions` not found (library API mismatch)
- These are expected since S1-02/S1-04/S1-05 are concurrent work

### What Needs Testing (Future):
- Unit tests for formatters (S1-12)
- Integration tests for progress rendering during actual push/pull (S1-13)
- Verify TTY detection on Windows/macOS/Linux
- Test JSON output in CI/CD pipelines

---

## Dependencies Satisfied

✅ **S1-01 complete:** Output/ directory exists in Oras.Cli  
✅ **Spectre.Console:** Already in Oras.Cli.csproj  
✅ **System.CommandLine:** Already in Oras.Cli.csproj  
⏳ **S1-02 (Options):** FormatOptions updated to work with TUI system  

---

## Next Steps

### For Dallas (Service Layer):
1. Update remaining Options classes to System.CommandLine 2.x API
2. Use `FormatOptions.CreateFormatter()` in command handlers
3. Integrate `ProgressRenderer` into PushService and PullService
4. Wire progress callbacks to `CopyGraphOptions` in copy operations

### For Hicks (Testing):
1. Write unit tests for `TextFormatter` and `JsonFormatter` (S1-12)
2. Mock `IAnsiConsole` to test TTY vs non-TTY behavior
3. Test `ProgressRenderer` callbacks and disposal
4. Verify JSON output structure matches expected format

### For Sprint 3 (Bishop):
1. Reuse `TreeNode` model for interactive manifest inspector
2. Enhance `ProgressRenderer` with live ETA calculations
3. Build TUI dashboard on top of formatters
4. Add `TuiFormatter` implementation for dashboard rendering

---

## File Manifest

```
src/Oras.Cli/Output/
├── IOutputFormatter.cs       (interface + TreeNode model)
├── TextFormatter.cs           (Spectre.Console implementation)
├── JsonFormatter.cs           (JSON output for --format json)
├── ProgressRenderer.cs        (progress bars + callbacks)
└── README.md                  (usage guide)

src/Oras.Cli/Options/
└── FormatOptions.cs           (modified: added CreateFormatter)

.squad/agents/bishop/
└── history.md                 (updated: learnings)

.squad/decisions/inbox/
└── bishop-output-design.md    (decision document)
```

---

## Conclusion

Sprint 1 TUI infrastructure (S1-03, S1-15) is **complete and ready for integration**. The output system provides a clean, testable, and extensible foundation for all CLI output, supporting both human-readable (text) and machine-readable (JSON) formats. Progress rendering is ready to be wired into push/pull services via the callback architecture.

The code follows all project conventions, uses the correct System.CommandLine 2.x API, and is documented for future developers.
