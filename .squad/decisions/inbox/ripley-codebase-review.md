# Architecture Review: oras-dotnet-cli

**Reviewer:** Ripley (Lead/Architect)  
**Date:** 2026-03-07  
**Scope:** Full codebase — 54 .cs files, ~5900 lines  
**Requested by:** Shiwei Zhang

---

## Executive Summary

The architecture is well-organized with clean separation between commands, services, credentials, and output formatting. The composable Options pattern and static command factory pattern are sound choices. However, there are **critical AOT compliance issues** that will cause silent runtime failures in the published native binary, plus several high-priority concerns around code duplication, DI anti-patterns, and a logic bug in `TextFormatter.SupportsInteractivity`.

---

## 🔴 CRITICAL — Will Break in AOT

### C-01: `JsonFormatter.WriteObject()` uses reflection-based serialization
**File:** `src/Oras.Cli/Output/JsonFormatter.cs:80-84`  
**Impact:** Every `--format json` command that calls `WriteObject()` will fail at runtime under AOT.  
**Details:** `JsonSerializer.Serialize(obj, _options)` requires runtime reflection to inspect the `object` type. The `[RequiresDynamicCode]` attribute correctly marks it as dangerous but doesn't fix it — the code will still be called.  
**Callers:** `WriteStatus()`, `WriteError()`, `WriteTable()`, `WriteTree()`, `WriteDescriptor()` — essentially ALL JSON output paths.  
**Fix:** Create an `OutputJsonContext : JsonSerializerContext` with `[JsonSerializable]` attributes for all output DTOs. Replace anonymous types with concrete record types. Use `JsonSerializer.Serialize(value, OutputJsonContext.Default.TypeName)` everywhere. Anonymous types (`new { status = "success", message }`) are fundamentally incompatible with source-generated JSON.

### C-02: `TextFormatter.WriteDescriptor()` uses reflection-based serialization
**File:** `src/Oras.Cli/Output/TextFormatter.cs:55-62`  
**Impact:** `WriteDescriptor()` will fail under AOT when displaying descriptor objects.  
**Fix:** Same as C-01 — use source-generated context with concrete types.

### C-03: `TextFormatter.WriteJson()` re-serializes `JsonDocument` via reflection
**File:** `src/Oras.Cli/Output/TextFormatter.cs:124-127`  
**Impact:** Pretty-printing JSON in text mode will fail under AOT.  
**Details:** `JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true })` on a `JsonDocument` uses reflection.  
**Fix:** Use `JsonSerializer.Serialize(doc, OutputJsonContext.Default.JsonDocument)` or use `Utf8JsonWriter` directly with `WriteIndented = true`.

### C-04: `TextFormatter.WriteObject()` uses reflection-based serialization
**File:** `src/Oras.Cli/Output/TextFormatter.cs:142-145`  
**Impact:** Same as C-01 for text output mode.  
**Fix:** Same as C-01.

### C-05: `ErrorHandler.HandleAsync()` marked `[RequiresDynamicCode]` — propagates to ALL commands
**File:** `src/Oras.Cli/ErrorHandler.cs:15-16, 62`  
**Impact:** `AnsiConsole.WriteException(ex)` in the debug-mode catch block may use reflection for exception formatting. Since `HandleAsync` wraps every command, this annotation propagates everywhere. The attribute annotation is only documentation — it does NOT prevent the call.  
**Fix:** Replace `AnsiConsole.WriteException(ex)` with `AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.ToString())}[/]")` which avoids Spectre's reflection-based exception renderer.

### C-06: `VersionCommand.GetLibraryVersion()` uses `AppDomain.CurrentDomain.GetAssemblies()` reflection
**File:** `src/Oras.Cli/Commands/VersionCommand.cs:67-74`  
**Impact:** Assembly enumeration via `AppDomain.CurrentDomain.GetAssemblies()` behaves differently under AOT. The LINQ query filtering by name may not find the OrasProject.Oras assembly as expected.  
**Fix:** Use `typeof(OrasProject.Oras.SomeKnownType).Assembly.GetName().Version` to get the version directly via a type reference, avoiding dynamic assembly scanning.

### C-07: Anonymous types used as JSON output in commands
**Files:** `BackupCommand.cs:152-161`, `RestoreCommand.cs:135-143`, `CopyCommand.cs:124-132`  
**Impact:** These commands create anonymous objects and pass them to `formatter.WriteObject()`, which as noted in C-01/C-04 calls `JsonSerializer.Serialize(obj)` — this will fail under AOT.  
**Fix:** Define concrete `record` types (e.g., `CopyResult`, `BackupResult`, `RestoreResult`) and register them in the source-generated JSON context.

---

## 🟠 HIGH — Bugs, Design Issues

### H-01: `TextFormatter.SupportsInteractivity` logic is INVERTED
**File:** `src/Oras.Cli/Output/TextFormatter.cs:19`  
**Code:** `public bool SupportsInteractivity => !_console.Profile.Capabilities.Interactive;`  
**Impact:** Returns `true` when the console is NOT interactive, and `false` when it IS. This means `BlobDeleteCommand` and `ManifestDeleteCommand` will prompt for confirmation when running non-interactively (piped) and skip confirmation in real terminals.  
**Fix:** Remove the `!` negation: `public bool SupportsInteractivity => _console.Profile.Capabilities.Interactive;`

### H-02: Service resolution uses `typeof()` + cast instead of generic `GetRequiredService<T>()`
**Files:** ALL command files (LoginCommand:32, LogoutCommand:27, PushCommand:44, PullCommand:55, CopyCommand:76, etc.) and TUI files (Dashboard:20, RegistryBrowser:20)  
**Pattern:** `serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService ?? throw new InvalidOperationException(...)`  
**Impact:** Verbose, error-prone, and doesn't leverage the DI container properly. The `typeof()` + `as` + null check pattern is 3x more code than needed.  
**Fix:** Add `using Microsoft.Extensions.DependencyInjection;` and use `serviceProvider.GetRequiredService<IRegistryService>()`. This does the same thing in one call and throws a more descriptive exception.

### H-03: `DockerConfigStore` is directly instantiated, bypassing DI
**Files:** `CredentialService.cs:11`, `Dashboard.cs:22`, `RegistryBrowser.cs:22`  
**Impact:** Three separate `new DockerConfigStore()` instances. `DockerConfigStore` cannot be mocked for testing, and its `_configPath` is hardcoded to the default. The TUI and CredentialService each create their own instance.  
**Fix:** Register `DockerConfigStore` as a singleton in DI (in `ServiceCollectionExtensions`). Inject it into `CredentialService`, `Dashboard`, and `RegistryBrowser`.

### H-04: `CredentialService.ValidateCredentialsAsync()` creates a `new RegistryService(this)` internally
**File:** `src/Oras.Cli/Services/CredentialService.cs:27`  
**Impact:** Circular dependency smell — `CredentialService` instantiates `RegistryService` directly, bypassing DI. If `RegistryService` gains more dependencies, this will break. Also creates a second instance when one already exists in the container.  
**Fix:** Inject `IRegistryService` into `CredentialService` constructor, or accept it as a parameter for `ValidateCredentialsAsync`. (Watch for circular DI — may need `Lazy<IRegistryService>` since `RegistryService` depends on `ICredentialService`.)

### H-05: `IOutputFormatter` is not registered in DI
**File:** `src/Oras.Cli/Services/ServiceCollectionExtensions.cs`  
**Impact:** `IOutputFormatter` instances are created via `FormatOptions.CreateFormatter()` in every command. There's no way to override the formatter for testing without modifying command code.  
**Fix:** Consider a factory approach: register `IOutputFormatterFactory` in DI. For now, the `FormatOptions.CreateFormatter()` pattern is acceptable since formatters are stateless, but flag for future testability work.

---

## 🟡 MEDIUM — Code Quality, Duplication

### M-01: `NormalizeRegistry()` duplicated in `LoginCommand` and `LogoutCommand`
**Files:** `LoginCommand.cs:106-121`, `LogoutCommand.cs:46-61`  
**Impact:** Identical 15-line method duplicated. If Docker Hub normalization changes, both must be updated.  
**Fix:** Extract to a shared `ReferenceHelper` static class (or into a `Services/RegistryHelper.cs`).

### M-02: `FormatSize()` duplicated 3× across the codebase
**Files:** `BackupCommand.cs:195-206`, `ManifestInspector.cs:631+`, `ProgressRenderer.cs:172-183`  
**Impact:** Same byte-formatting logic in three places.  
**Fix:** Extract to a shared `FormatHelper.FormatSize()` utility.

### M-03: `ExtractTag()` / `ExtractDigest()` / `ParseReference()` duplicated across commands
**Files:** `PullCommand.cs:127-155`, `PushCommand.cs:158-169`, `TagCommand.cs:72-108`  
**Impact:** Three different reference-parsing implementations with slightly different behavior. `PullCommand.ExtractTag` defaults to "latest"; `PushCommand.ExtractTag` returns null; `TagCommand.ParseReference` returns a 4-tuple.  
**Fix:** Create a `ReferenceParser` class with a single `Parse(string reference)` method returning a structured `OciReference` record with `Registry`, `Repository`, `Tag`, `Digest` fields. Use consistently across all commands.

### M-04: Commands repeat the same boilerplate pattern for service resolution + option parsing
**Impact:** Every command follows: `Create()` → add args → add options → `SetAction()` → `ErrorHandler.HandleAsync()` → resolve services → parse options. ~20 lines of setup boilerplate per command.  
**Fix:** Consider a base pattern (not class — keep static factories) that extracts common setup. A `CommandBuilder<TOptions>` helper could reduce boilerplate while preserving the static factory pattern.

### M-05: `CommonOptions` class exists but is never used
**File:** `src/Oras.Cli/Options/CommonOptions.cs`  
**Impact:** `--debug` and `--verbose` options are defined but never applied to any command. `GetValues()` method returns the options object unchanged — it does nothing.  
**Fix:** Either wire `CommonOptions` into the root command (via `ApplyTo(rootCommand)`) or remove it until implemented.

### M-06: `TargetOptions` class exists but is never used by any command
**File:** `src/Oras.Cli/Options/TargetOptions.cs`  
**Impact:** Dead code. Commands that need a target reference use inline `Argument<string>` instead.  
**Fix:** Remove `TargetOptions` or refactor commands to use it.

### M-07: `CommandExtensions.GetValue<T>()` overloads are redundant
**File:** `src/Oras.Cli/CommandExtensions.cs:27-38`  
**Impact:** Both `GetValue<T>(Argument<T>)` and `GetValue<T>(Option<T>)` simply delegate to the existing `ParseResult.GetValue<T>()` methods. They add no behavior — they're identity wrappers.  
**Fix:** Remove these extension methods. They were likely created for an older API shape and are now unnecessary with System.CommandLine 2.x.

### M-08: `CredentialService.RemoveCredentialsAsync()` silently swallows all exceptions
**File:** `src/Oras.Cli/Services/CredentialService.cs:59-66`  
**Impact:** Any error during credential removal is silently ignored. The `catch` block has a comment justifying it, but this means filesystem permission errors, config corruption, etc. are all hidden from the user during logout.  
**Fix:** Add logging or at least debug output in the catch block. Consider re-throwing non-expected exceptions.

---

## 🟢 LOW — Style, Conventions, Minor

### L-01: Namespace root is `Oras` but assembly is `oras` (lowercase)
**File:** `oras.csproj:7-8`  
**Impact:** `RootNamespace=Oras` with `AssemblyName=oras`. This is fine for the binary name but creates a subtle inconsistency. No functional issue.

### L-02: Exception hierarchy has empty parameterless constructors
**File:** `src/Oras.Cli/OrasException.cs:22-25, 38-40, 53-55, 67-69`  
**Impact:** `OrasAuthenticationException()`, `OrasNetworkException()`, `OrasUsageException()`, and `OrasException()` have empty constructors that produce exceptions with no message or recommendation. These serve no purpose — they're likely added for serialization compliance but are never used and shouldn't be.  
**Fix:** Remove parameterless constructors if not needed for serialization.

### L-03: `ProgressCallbackAdapter` is a thin wrapper with no added value
**File:** `src/Oras.Cli/Output/ProgressRenderer.cs:232-255`  
**Impact:** `ProgressCallbackAdapter` just delegates to `ProgressRenderer` methods with identical signatures. It exists only to implement `IProgressCallback`, but `ProgressRenderer` could implement that interface directly.  
**Fix:** Have `ProgressRenderer` implement `IProgressCallback` directly, or keep the adapter if separation of concerns between rendering and callback interface is intentional.

### L-04: `TuiCache` stores `object` values — type-unsafe
**File:** `src/Oras.Cli/Tui/TuiCache.cs:25`  
**Impact:** Boxing value types, no compile-time type safety. The `Get<T>` method casts `object` back to `T?`.  
**Fix:** Consider a type-safe cache using `ConcurrentDictionary<string, Func<object>>` or a typed wrapper pattern. Low priority since this is internal.

### L-05: `TrimmerRoots.xml` references `Spectre.Console.Json` namespace that may not exist
**File:** `src/Oras.Cli/TrimmerRoots.xml:5-7`  
**Impact:** The trimmer root references `Spectre.Console.Json.JsonText` and `Spectre.Console.Json` namespace, but `Spectre.Console.Json` is a separate NuGet package that is NOT in the project dependencies. This trimmer root does nothing.  
**Fix:** Either add the `Spectre.Console.Json` package (if JSON syntax highlighting is desired) or remove the trimmer root entry.

### L-06: `ParseResult` argument in `CommonOptions.GetValues()` is unused
**File:** `src/Oras.Cli/Options/CommonOptions.cs:41-44`  
**Impact:** Method accepts `ParseResult` but ignores it — returns the options object unchanged.  
**Fix:** Remove this dead method.

---

## Structural Assessment

### What's Good
- **Clean command pattern**: Static factory classes with `Create(IServiceProvider)` — no base class coupling, easy to understand.
- **Options composition**: `RemoteOptions`, `PackerOptions`, `PlatformOptions`, `FormatOptions` are composable and reusable across commands.
- **Credential store design**: Docker config.json + native credential helper protocol is well-implemented with source-generated JSON context.
- **Error hierarchy**: `OrasException` → `OrasAuthenticationException`/`OrasUsageException`/`OrasNetworkException` with exit code differentiation (1 vs 2) is clean.
- **TUI separation**: `Tui/` namespace is cleanly separated from CLI commands — good boundary.

### What Needs Attention
- **AOT compliance is fundamentally broken in the output layer** — the JSON formatter cannot serialize `object` types under AOT. This requires defining concrete DTOs for all command outputs.
- **Reference parsing is fragmented** — needs a single `OciReference` parser.
- **DI is half-used** — services are registered but resolved via anti-patterns. `DockerConfigStore` bypasses DI entirely.

---

## Prioritized Refactoring Plan

| Priority | Item | Effort | Impact |
|----------|------|--------|--------|
| **P0** | C-01 through C-07: Fix ALL AOT serialization issues | 2-3 days | Ship-blocking — AOT binary will crash |
| **P0** | H-01: Fix inverted `SupportsInteractivity` | 10 min | Logic bug — delete confirmation broken |
| **P1** | H-02: Replace `typeof()` + cast with `GetRequiredService<T>()` | 1 hour | Clean up all 20+ call sites |
| **P1** | H-03: Register `DockerConfigStore` in DI | 30 min | Testability, single instance |
| **P1** | H-04: Fix circular dependency in `CredentialService` | 30 min | Design smell |
| **P2** | M-01: Extract `NormalizeRegistry` to shared helper | 15 min | DRY |
| **P2** | M-02: Extract `FormatSize` to shared utility | 15 min | DRY |
| **P2** | M-03: Create unified `ReferenceParser` | 1 hour | Correctness + DRY |
| **P3** | M-05/M-06: Remove unused `CommonOptions`/`TargetOptions` | 10 min | Dead code |
| **P3** | M-07: Remove redundant `CommandExtensions.GetValue` | 10 min | Dead code |
| **P3** | L-02 through L-06: Style cleanup | 30 min | Polish |

### Recommended AOT Fix Strategy (P0)

1. **Define output DTOs** in a new `src/Oras.Cli/Output/OutputModels.cs`:
   ```csharp
   internal record StatusResult(string Status, string Message);
   internal record ErrorResult(string Status, string Error, string? Recommendation);
   internal record CopyResult(string Source, string Destination, bool Recursive, int Concurrency, string Platform, string Status);
   internal record BackupResult(string Reference, string Output, int Layers, string TotalSize, bool Recursive, string Platform, string Status);
   internal record RestoreResult(string Source, string Destination, bool Recursive, int Concurrency, string Status);
   internal record TableResult(object[] Items);
   ```

2. **Create `OutputJsonContext`** in `src/Oras.Cli/Output/OutputJsonContext.cs`:
   ```csharp
   [JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
   [JsonSerializable(typeof(StatusResult))]
   [JsonSerializable(typeof(ErrorResult))]
   [JsonSerializable(typeof(CopyResult))]
   // ... etc for all output types
   internal partial class OutputJsonContext : JsonSerializerContext;
   ```

3. **Refactor `JsonFormatter` and `TextFormatter`** to accept typed parameters instead of `object`.

---

*Reviewed by Ripley — Lead/Architect*
