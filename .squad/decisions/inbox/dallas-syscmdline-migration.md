# System.CommandLine 2.0.3 Migration

**Date:** 2026-03-06  
**Agent:** Dallas (Core Dev)  
**Status:** ✅ Complete  
**Impact:** All CLI code & tests

## Context

The oras-dotnet-cli project was initially developed against System.CommandLine 2.0.0-beta4, which has significant API differences from the stable 2.0.3 release. The package version was updated in Directory.Packages.props, requiring a complete code migration to match the new API surface.

## Problem

System.CommandLine beta4 and stable 2.0.3 are incompatible:

1. **Option construction** changed from named parameters + `AddAlias()` to constructor aliases + object initializers
2. **Default values** changed from `SetDefaultValue()` to `DefaultValueFactory` property
3. **Value constraints** changed from `FromAmong()` to `AcceptOnlyFromAmong()`
4. **Handler registration** changed from `SetHandler(InvocationContext)` to `SetAction(ParseResult, CancellationToken)`
5. **Value retrieval** changed from `GetValueForOption/GetValueForArgument` to unified `GetValue()`
6. **Command invocation** changed from `command.InvokeAsync(args)` to `command.Parse(args).InvokeAsync()`
7. **Test infrastructure** API `System.CommandLine.IO.TestConsole` was removed entirely

## Solution

Performed systematic migration across entire codebase following official 2.0.3 patterns:

### API Pattern Changes

#### Option Creation (6 classes migrated)
```csharp
// Before (beta4)
var option = new Option<bool>(
    name: "--verbose",
    description: "Enable verbose output");
option.AddAlias("-v");

// After (2.0.3)
var option = new Option<bool>("--verbose", "-v")
{
    Description = "Enable verbose output"
};
```

#### Default Values
```csharp
// Before
ConcurrencyOption = new Option<int>(
    name: "--concurrency",
    getDefaultValue: () => 3);

// After
ConcurrencyOption = new Option<int>("--concurrency")
{
    DefaultValueFactory = _ => 3
};
```

#### Value Constraints
```csharp
// Before
FormatOption.FromAmong("text", "json");

// After
FormatOption.AcceptOnlyFromAmong("text", "json");
```

#### Argument Creation
```csharp
// Before
var arg = new Argument<string[]>(
    name: "files",
    description: "Files to push");
arg.Arity = ArgumentArity.ZeroOrMore;

// After
var arg = new Argument<string[]>("files")
{
    Description = "Files to push",
    Arity = ArgumentArity.ZeroOrMore
};
```

#### Handler Registration
```csharp
// Before (CommandExtensions.cs compatibility shim)
command.SetHandler(async (InvocationContext context) =>
{
    var exitCode = await action(context.ParseResult);
    context.ExitCode = exitCode;
});

// After
command.SetAction(async (parseResult, cancellationToken) =>
{
    var exitCode = await action(parseResult);
    Environment.ExitCode = exitCode;
});
```

#### Value Retrieval
```csharp
// Before (CommandExtensions.cs GetValue methods)
parseResult.GetValueForOption(option);
parseResult.GetValueForArgument(arg);

// After (native API - extensions now just call through)
parseResult.GetValue(option);
parseResult.GetValue(arg);
```

#### Command Invocation
```csharp
// Before (Program.cs)
return await rootCommand.InvokeAsync(args);

// After
return await rootCommand.Parse(args).InvokeAsync();
```

#### Test Infrastructure
```csharp
// Before (CommandTestHelper.cs)
using System.CommandLine.IO;
private readonly TestConsole _console;
ExitCode = await command.InvokeAsync(args, _console);

// After (TestConsole removed in 2.0.3)
private readonly StringWriter _standardOutput;
private readonly StringWriter _standardError;
Console.SetOut(_standardOutput);
ExitCode = await command.Parse(args).InvokeAsync();
Console.SetOut(originalOut);
```

### Files Modified

**Option Classes (6):**
- `src/Oras.Cli/Options/CommonOptions.cs`
- `src/Oras.Cli/Options/RemoteOptions.cs`
- `src/Oras.Cli/Options/TargetOptions.cs`
- `src/Oras.Cli/Options/PackerOptions.cs`
- `src/Oras.Cli/Options/FormatOptions.cs`
- `src/Oras.Cli/Options/PlatformOptions.cs`

**Command Classes (5):**
- `src/Oras.Cli/Commands/LoginCommand.cs`
- `src/Oras.Cli/Commands/LogoutCommand.cs`
- `src/Oras.Cli/Commands/PushCommand.cs`
- `src/Oras.Cli/Commands/PullCommand.cs`
- `src/Oras.Cli/Commands/VersionCommand.cs`

**Infrastructure (3):**
- `src/Oras.Cli/CommandExtensions.cs` - Removed `InvocationContext` usage
- `src/Oras.Cli/Program.cs` - Updated invocation pattern
- `test/oras.Tests/Helpers/CommandTestHelper.cs` - Replaced TestConsole with StringWriter

## Results

**Build Status:**
- ✅ 0 compilation errors
- ⚠️ 138 analyzer warnings (pre-existing, unrelated to migration)

**Test Status:**
- ✅ All 15 unit tests passing
- ✅ Test infrastructure verified working with new console capture approach

## Decision

**Adopt System.CommandLine 2.0.3 stable release as the CLI framework.**

### Rationale

1. **Stability:** 2.0.3 is the first stable release after years of beta
2. **Long-term Support:** Microsoft committed to maintaining the 2.x API surface
3. **API Improvements:** New patterns are more idiomatic (object initializers, unified GetValue)
4. **Documentation:** Official docs now align with 2.0.3 patterns

### Alternatives Considered

1. **Stay on beta4:** Not viable - no bug fixes, abandoned by Microsoft
2. **Switch to Spectre.Console.Cli:** High churn, would lose existing work
3. **Roll own parser:** Unnecessary reinvention

## Lessons Learned

1. **Namespace Removal:** System.CommandLine.IO namespace completely removed in 2.0.3. Any test code using TestConsole must migrate to console redirection patterns.

2. **SetAction Signature:** The new `SetAction(ParseResult, CancellationToken)` signature is cleaner than beta's `SetHandler(InvocationContext)`, but requires wrapper to integrate with error handling that returns exit codes.

3. **Parse Then Invoke:** The 2.0.3 pattern of `command.Parse(args).InvokeAsync()` is more explicit about two-phase processing (parsing vs execution) compared to beta's single `InvokeAsync(args)`.

4. **Extension Method Strategy:** Keeping `CommandExtensions.GetValue()` wrappers provides a stable internal API even though 2.0.3 has native GetValue. This insulates the codebase from future changes.

5. **Object Initializer Pattern:** The shift to object initializers for options/arguments is more consistent with modern C# conventions and makes code more maintainable.

## Future Considerations

1. **System.CommandLine 3.x:** Monitor for 3.x announcements, but 2.x API is now stable baseline
2. **Test Infrastructure:** Consider abstracting test helpers further if System.CommandLine.Testing package emerges
3. **Spectre.Console Integration:** Explore if Spectre.Console.Cli features can complement System.CommandLine 2.x

## References

- [System.CommandLine 2.0.3 Release](https://www.nuget.org/packages/System.CommandLine/2.0.3)
- Official Microsoft docs provided in task specification
- Project: `oras-dotnet-cli` - Cross-platform .NET 10 CLI for OCI Registry As Storage

---

**Migration Completed:** 2026-03-06  
**Verification:** Build passed, all tests green  
**Sign-off:** Dallas (Core Dev)
