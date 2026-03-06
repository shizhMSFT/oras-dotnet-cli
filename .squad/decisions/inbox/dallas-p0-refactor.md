# Dallas P0/P1 Output Refactor

**Date:** 2026-03-07  
**Owner:** Dallas (Core Developer)  
**Requested by:** Shiwei Zhang  
**Scope:** Output AOT compliance and CLI P1 fixes

---

## Decision

1. **Source-generated JSON output:** Introduce concrete output records (StatusResult, ErrorResult, CopyResult, BackupResult, RestoreResult, DescriptorResult, TableResult, TreeNodeResult) and wire `OutputJsonContext` for all formatter serialization.
2. **Typed formatter API:** Replace `WriteObject(object)` with `WriteObject<T>(T, JsonTypeInfo<T>)` so JSON/text formatters can serialize with `OutputJsonContext` without reflection.
3. **P0/P1 CLI corrections:** Fix text interactivity detection, update ErrorHandler cancellation handling + debug exception isolation, replace VersionCommand assembly lookup with a direct OrasProject type reference, remove `--force -f` alias conflict, close PushCommand streams with `await using`, remove the duplicate artifact-type option in attach, and dispose the ServiceProvider.

## Rationale

- Native AOT and trimming require source-generated JSON and concrete types; reflection-based serialization fails at runtime.
- Typed formatter APIs preserve structured output across text and JSON while keeping output models explicit and documentation-like.
- The P1 fixes eliminate CLI option conflicts and resource leaks without touching the TUI layer.

## Files Updated

- `src/Oras.Cli/Output/OutputModels.cs`
- `src/Oras.Cli/Output/OutputJsonContext.cs`
- `src/Oras.Cli/Output/IOutputFormatter.cs`
- `src/Oras.Cli/Output/JsonFormatter.cs`
- `src/Oras.Cli/Output/TextFormatter.cs`
- `src/Oras.Cli/Output/README.md`
- `src/Oras.Cli/Commands/CopyCommand.cs`
- `src/Oras.Cli/Commands/BackupCommand.cs`
- `src/Oras.Cli/Commands/RestoreCommand.cs`
- `src/Oras.Cli/Commands/VersionCommand.cs`
- `src/Oras.Cli/ErrorHandler.cs`
- `src/Oras.Cli/Commands/BlobDeleteCommand.cs`
- `src/Oras.Cli/Commands/ManifestDeleteCommand.cs`
- `src/Oras.Cli/Commands/PushCommand.cs`
- `src/Oras.Cli/Commands/AttachCommand.cs`
- `src/Oras.Cli/Program.cs`

## Tests

- `dotnet build src\Oras.Cli\oras.csproj --no-restore`
