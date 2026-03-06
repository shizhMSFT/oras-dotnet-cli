# Dallas — Codebase Review: CLI Commands, Services, Credentials, Output, Options

**Reviewer:** Dallas (Core Developer)  
**Date:** 2026-03-06  
**Scope:** All 21 command files, 9 service files, 4 credential files, 5 output files, 6 option files, cross-cutting infrastructure  
**Build status at time of review:** ✅ 0 errors, 124 warnings (all pre-existing CA1707/CA1307/CA1031 in tests)

---

## Executive Summary

The codebase has a solid architectural skeleton: composable options, a service layer, structured error handling, and DI wiring. However, it has **systemic issues** that will compound as the library integration stubs are replaced with real logic. The most critical findings are:

1. **AOT-breaking reflection in formatters** (KNOWN BUG, confirmed — P0)
2. **CancellationToken dropped in 19 of 21 commands** (P0)
3. **Service resolution via anti-pattern** — manual `GetService()` cast instead of DI constructor injection (P1)
4. **Massive command boilerplate duplication** — every command repeats 10–15 lines of identical service resolution + option reading (P1)
5. **Resource leak** in PushCommand — `FileStream` not in `using` block (P1)
6. **NormalizeRegistry duplicated** across LoginCommand and LogoutCommand (P2)
7. **`TextFormatter.SupportsInteractivity` logic is inverted** (P1 — will break `--force` prompts)

---

## 1. Commands (src/Oras.Cli/Commands/*.cs)

### 1.1 CancellationToken Not Propagated — ALL commands (P0)

**Files:** Every command file  
**What's wrong:** `CommandExtensions.SetAction` wraps `Func<ParseResult, Task<int>>` but the **CancellationToken from the native `SetAction((parseResult, cancellationToken) => ...)` is never exposed** to command implementations. Every async operation (registry calls, file I/O, credential helpers) uses `CancellationToken.None` or no token at all.

`CommandExtensions.cs:15-22`:
```csharp
// CURRENT — cancellationToken is received but never forwarded
public static void SetAction(this Command command, Func<ParseResult, Task<int>> action)
{
    command.SetAction(async (parseResult, cancellationToken) =>
    {
        var exitCode = await action(parseResult).ConfigureAwait(false);
        Environment.ExitCode = exitCode;
    });
}
```

**Recommended fix:** Change the delegate signature to pass through the token:
```csharp
public static void SetAction(this Command command, Func<ParseResult, CancellationToken, Task<int>> action)
{
    command.SetAction(async (parseResult, cancellationToken) =>
    {
        var exitCode = await action(parseResult, cancellationToken).ConfigureAwait(false);
        Environment.ExitCode = exitCode;
    });
}
```
Then update every command handler: `command.SetAction(async (parseResult, ct) => { ... })`.

**Impact:** Users cannot Ctrl+C to cancel long operations. Credential helper processes won't be killed. Network requests will hang until timeout.

---

### 1.2 Service Resolution Anti-Pattern — ALL commands (P1)

**Files:** Every command except VersionCommand  
**What's wrong:** Every command does manual service locator calls:
```csharp
var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
    ?? throw new InvalidOperationException("Registry service not available");
```

This pattern:
- Bypasses compile-time type safety (non-generic `GetService` + cast)
- Is repeated verbatim in 20 commands
- Creates a service locator anti-pattern — DI exists but isn't used idiomatically

**Recommended fix:** Use the generic `GetRequiredService<T>()` extension:
```csharp
using Microsoft.Extensions.DependencyInjection;
// ...
var registryService = serviceProvider.GetRequiredService<IRegistryService>();
```
Even better, extract a helper that commands call:
```csharp
internal static class ServiceProviderExtensions
{
    public static T Require<T>(this IServiceProvider sp) where T : notnull
        => sp.GetRequiredService<T>();
}
```

---

### 1.3 Duplicated NormalizeRegistry — LoginCommand.cs + LogoutCommand.cs (P2)

**Files:** `LoginCommand.cs:106-121`, `LogoutCommand.cs:46-61`  
**What's wrong:** Identical `NormalizeRegistry()` method duplicated in both files.

**Recommended fix:** Extract to a shared utility:
```csharp
// In a new file: Commands/ReferenceHelper.cs or in CommandExtensions.cs
internal static class ReferenceHelper
{
    public static string NormalizeRegistry(string registry) { ... }
    public static void ValidateReference(string reference, string paramName) { ... }
    public static (string registry, string repository, string? tag, string? digest) ParseReference(string reference) { ... }
    public static string? ExtractTag(string reference) { ... }
    public static string? ExtractDigest(string reference) { ... }
}
```

Note: `CopyCommand.ValidateReference` is already `internal static` and reused by BackupCommand/RestoreCommand — good. But `TagCommand.ParseReference`, `PullCommand.ExtractTag/ExtractDigest`, and `PushCommand.ExtractTag` are all separate implementations of overlapping reference-parsing logic.

---

### 1.4 Resource Leak in PushCommand — PushCommand.cs:89 (P1)

**File:** `PushCommand.cs:89-104`
```csharp
var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
// ...
await repo.Blobs.PushAsync(descriptor, fileStream).ConfigureAwait(false);
fileStream.Close(); // Not exception-safe
```

**What's wrong:** If `PushAsync` throws, `fileStream` is never closed. The `Close()` call is not in a `finally` block.

**Recommended fix:**
```csharp
await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
await repo.Blobs.PushAsync(descriptor, fileStream).ConfigureAwait(false);
```

---

### 1.5 Unreachable Code After `throw` — Multiple commands (P2)

**Files:** `TagCommand.cs:61`, `PushCommand.cs:115`, `PullCommand.cs:107`, and ~10 more stub commands  
**What's wrong:** Code after `throw new NotImplementedException(...)` is dead (commented-out return statements, post-throw logic). While this is expected for stubs, the compiler warnings are suppressed silently. When implementing real logic, it's easy to miss removing the throw.

**Recommendation:** Add `// STUB:` markers with `#pragma warning disable/restore CS0162` around unreachable code to make them auditable, or use a sentinel method:
```csharp
[DoesNotReturn]
private static void ThrowNotImplemented(string operation)
    => throw new NotImplementedException($"{operation} awaiting oras-dotnet library integration");
```

---

### 1.6 Duplicate Option Construction Per Command (P2)

**Files:** All commands that use `RemoteOptions`, `FormatOptions`, `PlatformOptions`  
**What's wrong:** Each command creates `new RemoteOptions()`, `new FormatOptions()`, etc. These are lightweight but the pattern means every command independently owns its options. If option defaults change, you'd need to update each constructor.

**Currently not a problem** because options are value types with no shared state, but something to watch.

---

### 1.7 `AttachCommand` Has Duplicate `--artifact-type` Option (P2)

**File:** `AttachCommand.cs:46-50`  
**What's wrong:** AttachCommand adds its own `--artifact-type` option AND also applies `PackerOptions` which already contains `ArtifactTypeOption`. This will cause a System.CommandLine duplicate-option error at runtime.

**Recommended fix:** Remove the local `artifactTypeOpt` and use `packerOptions.ArtifactTypeOption` instead, with a custom validator for the required constraint.

---

### 1.8 `ErrorHandler.HandleAsync` Missing `OperationCanceledException` (P2)

**File:** `ErrorHandler.cs:39-45`  
**What's wrong:** Only `TaskCanceledException` is caught. `OperationCanceledException` (its base class) is not caught separately. Many .NET APIs throw `OperationCanceledException` directly (including `CancellationToken.ThrowIfCancellationRequested()`).

**Recommended fix:** Catch `OperationCanceledException` instead (which covers both):
```csharp
catch (OperationCanceledException)
{
    WriteError("Operation cancelled", "The operation was interrupted.");
    return 130; // Unix convention for SIGINT
}
```

---

### 1.9 ErrorHandler.WriteError Doesn't Escape Markup (P2)

**File:** `ErrorHandler.cs:71`
```csharp
AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
```

**What's wrong:** If `message` contains Spectre.Console markup characters (`[`, `]`), it will be interpreted as markup and may crash or produce garbled output. Registry error messages often contain brackets.

**Recommended fix:**
```csharp
AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
```

---

### 1.10 Login Uses `CancellationToken.None` Instead of Propagated Token (P1)

**File:** `LoginCommand.cs:82`
```csharp
await registryService.CreateRegistryAsync(
    registry, username, password, plainHttp, insecure,
    CancellationToken.None).ConfigureAwait(false);
```

This is a specific instance of finding 1.1 — login network calls are uncancellable.

---

## 2. Services (src/Oras.Cli/Services/*.cs)

### 2.1 CredentialService Creates RegistryService Internally — Circular Dependency Risk (P1)

**File:** `CredentialService.cs:26-33`
```csharp
var registryService = new RegistryService(this);
```

**What's wrong:** `CredentialService.ValidateCredentialsAsync` manually constructs a `RegistryService(this)`, bypassing DI. This creates a tight circular coupling: RegistryService depends on ICredentialService, and CredentialService instantiates RegistryService.

**Recommended fix:** Inject `IRegistryService` into `CredentialService` via constructor, or move validation logic into a separate `ICredentialValidator` service that sits above both.

---

### 2.2 Services Are Stubs That `await Task.CompletedTask` Then Throw (P2)

**Files:** `RegistryService.cs:27`, `PushService.cs:32`, `PullService.cs:29`
```csharp
await Task.CompletedTask.ConfigureAwait(false); // Suppress async warning
throw new NotImplementedException(...);
```

**What's wrong:** `await Task.CompletedTask` is a no-op pattern to suppress CS1998 (async method lacks await). This is fine for stubs but **the methods should be converted to synchronous `throw` with `Task.FromException<T>()` return** to avoid unnecessary state machine allocation, OR kept as-is with a clear `// STUB` comment.

**Not urgent** but when implementing real logic, remove the `Task.CompletedTask` line.

---

### 2.3 IPushService/IPullService Are Not Used By Their Respective Commands (P2)

**Files:** `PushCommand.cs` uses `IRegistryService` directly; `PullCommand.cs` uses `IRegistryService` directly  
**What's wrong:** `IPushService` and `IPullService` exist in the DI container but the actual `PushCommand` and `PullCommand` don't use them — they inline the push/pull logic instead.

**Recommended fix:** Either use the services (move logic into PushService/PullService and call from commands) or remove the unused interfaces until they're needed.

---

### 2.4 ServiceCollectionExtensions: Inconsistent Lifetimes (P3)

**File:** `ServiceCollectionExtensions.cs:15-18`
```csharp
services.AddSingleton<ICredentialService, CredentialService>();
services.AddSingleton<IRegistryService, RegistryService>();
services.AddTransient<IPushService, PushService>();
services.AddTransient<IPullService, PullService>();
```

**What's wrong:** `CredentialService` is singleton but creates `new DockerConfigStore()` in its constructor — the config store reads from disk each time. If config changes between operations in the same process, the singleton won't pick up changes. Meanwhile, `PushService`/`PullService` are transient but stateless.

**Recommendation:** Make all services transient (CLI is short-lived) or all singleton. The distinction doesn't matter for a CLI but the inconsistency is confusing.

---

### 2.5 IRegistryService.CreateRepositoryAsync Missing CancellationToken (P2)

**File:** `IRegistryService.cs:24-30`
```csharp
Task<Repository> CreateRepositoryAsync(
    string reference,
    string? username = null,
    string? password = null,
    bool plainHttp = false,
    bool insecure = false,
    CancellationToken cancellationToken = default);
```

The signature is correct — it accepts `CancellationToken`. But callers (`PushCommand.cs:77-82`, `PullCommand.cs:76-81`) **don't pass a token**:
```csharp
var repo = await registryService.CreateRepositoryAsync(
    reference, username, password, plainHttp, insecure)
    .ConfigureAwait(false);
// Missing: CancellationToken argument
```

This is another instance of 1.1.

---

## 3. Credentials (src/Oras.Cli/Credentials/*.cs)

### 3.1 DockerConfigStore Load/Save Has Race Condition (P2)

**File:** `DockerConfigStore.cs:24-51`  
**What's wrong:** `LoadAsync` reads the entire file, then `SaveAsync` writes it back. If two CLI instances run concurrently (e.g., `oras login` in two terminals), one will overwrite the other's changes.

**Recommended fix:** For a CLI this is low-risk, but consider file locking:
```csharp
using var stream = new FileStream(_configPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
```
Or use advisory locking with a `.lock` file. Docker's own CLI has the same limitation, so this is acceptable with a documented caveat.

---

### 3.2 NativeCredentialHelper Has No Timeout (P1)

**File:** `NativeCredentialHelper.cs:111-166`  
**What's wrong:** `RunHelperAsync` calls `process.WaitForExitAsync(cancellationToken)` which respects cancellation — good. But if no CancellationToken is passed (and most callers pass `default`), a hung credential helper will block forever.

**Recommended fix:** Add a reasonable timeout:
```csharp
using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
```

---

### 3.3 NativeCredentialHelper Process Not Killed on Cancellation (P2)

**File:** `NativeCredentialHelper.cs:156`  
**What's wrong:** If `WaitForExitAsync` throws due to cancellation, the child process is still running. The `using` statement on the `Process` object will call `Dispose()`, but `Process.Dispose()` does **not** kill the process.

**Recommended fix:**
```csharp
try
{
    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    try { process.Kill(entireProcessTree: true); } catch { }
    throw;
}
```

---

### 3.4 CredentialJsonContext Naming Policy Mismatch (P2)

**File:** `CredentialJsonContext.cs:10-11`
```csharp
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
```

But `CredentialHelperResponse` and `CredentialHelperInput` use **PascalCase** `[JsonPropertyName]` attributes (`"Username"`, `"Secret"`, `"ServerURL"`).

**What's wrong:** The `CamelCase` naming policy from the source generator context would produce `username`, `secret`, `serverUrl` — but the `[JsonPropertyName]` attributes override this. So it works, but the `CamelCase` policy on the context is misleading for these types. It does affect `DockerConfig` serialization where property names use `[JsonPropertyName]` too, so the policy is effectively a no-op for all registered types.

**Recommendation:** Either remove the `PropertyNamingPolicy` from the context (since all types use explicit `[JsonPropertyName]`) or document why it's there.

---

### 3.5 DockerConfig.Auths Default Empty Dictionary — Good (No Issue)

The `Auths` property defaults to `new()` which prevents null-reference issues. This is correct.

---

## 4. Output (src/Oras.Cli/Output/*.cs)

### 4.1 AOT-Breaking Reflection in JsonFormatter and TextFormatter (P0 — KNOWN BUG)

**Files:** `JsonFormatter.cs:78-83`, `TextFormatter.cs:53-62`, `TextFormatter.cs:106-107`, `TextFormatter.cs:141`
```csharp
[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
public void WriteObject(object obj)
{
    var json = JsonSerializer.Serialize(obj, _options);
    _console.WriteLine(json);
}
```

**What's wrong:** `JsonSerializer.Serialize(object, JsonSerializerOptions)` uses reflection, which fails under Native AOT. The `[RequiresDynamicCode]` attributes suppress the warning but don't fix the problem — the serialization **will fail at runtime** when published as AOT.

**Recommended fix:** Create an `OutputJsonContext` source-generated serializer that covers all types passed to `WriteObject`:
```csharp
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(CopySummary))]  // define a record for each output shape
// ... etc
internal partial class OutputJsonContext : JsonSerializerContext;
```

Then change `WriteObject` to accept a `JsonTypeInfo<T>` parameter:
```csharp
public void WriteObject<T>(T obj, JsonTypeInfo<T> typeInfo)
{
    var json = JsonSerializer.Serialize(obj, typeInfo);
    _console.WriteLine(json);
}
```

Alternatively, accept a pre-serialized `string` and rename the method to `WriteSerializedJson`. This is a design decision — the current approach is fundamentally incompatible with the project's AOT goal.

---

### 4.2 TextFormatter.SupportsInteractivity Is Inverted (P1)

**File:** `TextFormatter.cs:19`
```csharp
public bool SupportsInteractivity => !_console.Profile.Capabilities.Interactive;
```

**What's wrong:** The `!` (NOT) operator inverts the logic. When the console IS interactive, this returns `false`. When it's NOT interactive, it returns `true`. This means the delete commands' confirmation prompts (`BlobDeleteCommand.cs:57`, `ManifestDeleteCommand.cs:57`) will show interactive prompts in non-interactive mode and skip them in interactive mode — **the exact opposite of the intended behavior**.

**Recommended fix:**
```csharp
public bool SupportsInteractivity => _console.Profile.Capabilities.Interactive;
```

---

### 4.3 TextFormatter.WriteJson Creates New JsonSerializerOptions Per Call (P3)

**File:** `TextFormatter.cs:57-58`, `TextFormatter.cs:126`
```csharp
var json = JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true });
```

**What's wrong:** `new JsonSerializerOptions` is allocated every call. The .NET JSON serializer caches metadata per options instance, so creating new ones prevents caching.

**Recommended fix:** Use a static/shared instance:
```csharp
private static readonly JsonSerializerOptions s_prettyOptions = new() { WriteIndented = true };
```

---

### 4.4 ProgressRenderer.Start Has Blocking Synchronous Call (P2)

**File:** `ProgressRenderer.cs:40-53`
```csharp
_console.Progress()
    .Start(ctx =>    // <-- synchronous Start, not StartAsync
    {
        _progressContext = ctx;
        _overallTask = ctx.AddTask(...);
    });
```

**What's wrong:** `Progress().Start()` is a synchronous method — it blocks the thread while the callback runs. Since the callback only sets up tasks, this is fine functionally, but the `_progressContext` is captured from within the callback and used later — **but `Start()` completes immediately** after the callback returns, so the `_progressContext` is stale. The progress context is only valid within the `Start` callback scope.

**Recommended fix:** Use `Progress().StartAsync()` with the actual work happening inside the callback, or switch to `AnsiConsole.Status()` (which the command files already use).

---

### 4.5 FormatSize Duplicated — BackupCommand + ProgressRenderer (P3)

**Files:** `BackupCommand.cs:196-206`, `ProgressRenderer.cs:172-183`  
**What's wrong:** Nearly identical `FormatSize(long bytes)` implementations.

**Recommended fix:** Extract to a shared utility in `Output/` or a `Utilities/` namespace.

---

## 5. Options (src/Oras.Cli/Options/*.cs)

### 5.1 CommonOptions.GetValues Does Nothing (P3)

**File:** `CommonOptions.cs:41-44`
```csharp
public static CommonOptions GetValues(ParseResult parseResult, CommonOptions options)
{
    return options; // Just returns the input unchanged
}
```

**What's wrong:** Dead code — never called, returns input unchanged.

**Recommended fix:** Remove.

---

### 5.2 CommonOptions Not Applied to Any Command (P2)

**File:** `CommonOptions.cs` is defined but **never used** in `Program.cs` or any command.  
**What's wrong:** `--debug` and `--verbose` options are defined but not wired to the root command or any subcommands. The `VersionCommand.cs:37` uses `ToLowerInvariant()` without verbose mode, and `ErrorHandler.cs:60` checks `ORAS_DEBUG` environment variable instead of the `--debug` option.

**Recommended fix:** Apply `CommonOptions` to the root command in `Program.cs` and read `--debug` in `ErrorHandler` instead of the environment variable.

---

### 5.3 PackerOptions.ConcurrencyOption Default Is 3, But CopyCommand/BackupCommand/RestoreCommand Use 5 (P3)

**Files:**
- `PackerOptions.cs:42`: default `3`
- `CopyCommand.cs:67`: default `5`
- `BackupCommand.cs:49`: default `5`
- `RestoreCommand.cs:44`: default `5`

**What's wrong:** Inconsistent default concurrency values. Commands that don't use `PackerOptions` define their own `--concurrency` with a different default.

**Recommended fix:** Standardize on one default (the Go CLI uses 3). Extract concurrency as a shared option.

---

### 5.4 FormatOptions `-f` Alias Conflicts With `--force` `-f` in Delete Commands (P1)

**Files:** `FormatOptions.cs:17` defines `--format, -f`. `BlobDeleteCommand.cs:34` and `ManifestDeleteCommand.cs:34` define `--force, -f`.

**What's wrong:** Both `FormatOptions` and the delete commands use `-f` as an alias. Since delete commands apply `FormatOptions` AND define `--force -f`, **this will cause a System.CommandLine error at runtime**: "Option '-f' is already defined."

**Recommended fix:** Change `--force` alias to something else (the Go CLI doesn't use `-f` for force — it uses `--force` only), or remove the `-f` alias from `--format`.

---

### 5.5 RemoteOptions: `--header` and `--resolve` Are Single-Value But Should Be Multi-Value (P3)

**File:** `RemoteOptions.cs:46-59`  
**What's wrong:** `HeaderOption` and `ResolveOption` are `Option<string?>` (single value). The Go CLI supports multiple `--header` flags. Should be `Option<string[]?>` with `AllowMultipleArgumentsPerToken = true`.

---

## 6. Cross-Cutting Concerns

### 6.1 CommandExtensions.GetValue Methods Are Redundant (P3)

**File:** `CommandExtensions.cs:27-38`  
**What's wrong:** The `GetValue<T>(ParseResult, Argument<T>)` and `GetValue<T>(ParseResult, Option<T>)` extension methods simply call `parseResult.GetValue(argument)` — they add zero functionality. In System.CommandLine 2.0.3, `parseResult.GetValue()` already exists as a native method.

**Recommended fix:** Remove these methods and use `parseResult.GetValue()` directly (which commands already do — these extensions are unused).

---

### 6.2 Program.cs: ServiceProvider Not Disposed (P2)

**File:** `Program.cs:18`
```csharp
var serviceProvider = services.BuildServiceProvider();
```

**What's wrong:** `ServiceProvider` implements `IDisposable`/`IAsyncDisposable`. It's never disposed, which means any `IDisposable` services won't be cleaned up.

**Recommended fix:**
```csharp
await using var serviceProvider = services.BuildServiceProvider();
```

---

### 6.3 OrasException Hierarchy: Parameterless Constructors Are Unusual (P3)

**File:** `OrasException.cs:22-25`, `:37-39`, `:51-53`, `:65-67`  
**What's wrong:** Each exception class has a parameterless constructor that initializes no `Recommendation`. These exist presumably for completeness but are never used and weaken the type contract (an `OrasAuthenticationException()` with no message is not useful).

**Recommendation:** Remove parameterless constructors or mark them `[Obsolete]` to discourage use.

---

### 6.4 ErrorHandler.HandleAsync Has `[RequiresDynamicCode]` Attribute (P3)

**File:** `ErrorHandler.cs:16`
```csharp
[RequiresDynamicCode("Calls Spectre.Console.AnsiConsole.WriteException(Exception, ExceptionFormats)")]
```

**What's wrong:** This AOT suppression attribute propagates to every caller. Since `HandleAsync` wraps every command, this effectively marks the entire CLI as requiring dynamic code. The attribute is only needed for the `ORAS_DEBUG` stack trace path.

**Recommended fix:** Move the debug stack trace into a separate `[RequiresDynamicCode]` method so the attribute doesn't spread:
```csharp
[RequiresDynamicCode("...")]
private static void WriteDebugException(Exception ex) => AnsiConsole.WriteException(ex);
```

---

## 7. Summary of Extractable Patterns

| Pattern | Current Locations | Recommended Extraction |
|---------|------------------|----------------------|
| `NormalizeRegistry()` | LoginCommand, LogoutCommand | `ReferenceHelper.NormalizeRegistry()` |
| `ValidateReference()` | CopyCommand (shared), BackupCommand, RestoreCommand | Already shared — good |
| `ParseReference()` | TagCommand | `ReferenceHelper.ParseReference()` |
| `ExtractTag()` / `ExtractDigest()` | PushCommand, PullCommand | `ReferenceHelper.ExtractTag/Digest()` |
| Service resolution boilerplate | 20 commands | Generic helper or base method |
| `FormatSize()` | BackupCommand, ProgressRenderer | `SizeFormatter.Format()` utility |
| Option reading + format init | 15+ commands | Consider a `CommandContext` record |

---

## 8. Priority Summary

| Priority | Count | Key Items |
|----------|-------|-----------|
| **P0** | 2 | AOT-breaking formatter reflection; CancellationToken not propagated |
| **P1** | 5 | Inverted interactivity flag; `-f` alias conflict; Service locator anti-pattern; FileStream leak; Credential helper timeout |
| **P2** | 8 | NormalizeRegistry duplication; Race condition in config; Process not killed; Dead code; CommonOptions unused; ServiceProvider not disposed; ErrorHandler markup escape; OperationCanceledException |
| **P3** | 6 | Redundant extensions; FormatSize duplication; Inconsistent defaults; Parameterless exceptions; Multi-value headers; Dead GetValues method |

---

## 9. Positive Patterns Worth Preserving

1. **Composable option classes** with `ApplyTo()` — clean and reusable
2. **Source-generated JSON for credentials** (`CredentialJsonContext`) — correct AOT approach
3. **Error hierarchy** with `Recommendation` field — excellent UX
4. **FormatOptions.CreateFormatter() factory** — clean polymorphism
5. **Internal static command classes** with `Create()` factory — testable and consistent
6. **NativeCredentialHelper protocol compliance** — proper docker-credential-helpers implementation
7. **DockerConfigStore.ListRegistriesAsync()** aggregating from 3 sources — thorough

---

*Review complete. All findings verified against source at build commit.*
