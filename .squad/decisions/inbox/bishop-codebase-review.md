# TUI Layer Code Review — Bishop

**Date:** 2026-03-07
**Reviewer:** Bishop (TUI Developer)
**Scope:** All files in `src/Oras.Cli/Tui/`
**Severity scale:** 🔴 Critical · 🟠 High · 🟡 Medium · 🔵 Low · ⚪ Informational

---

## 1. Dashboard.cs (851 lines)

### 🟠 HIGH — God-class: action handlers bloat the file beyond its role

**Lines:** 199–843
**What's wrong:** Dashboard is supposed to be the *entry point* — show a menu, dispatch actions. Instead, it contains ~650 lines of fully self-contained action handlers (`HandleCopyArtifactAsync`, `HandleBackupArtifactAsync`, `HandleRestoreArtifactAsync`, `HandlePushArtifactAsync`, `HandlePullArtifactAsync`, `HandleTagArtifactAsync`). Each handler duplicates the same progress-bar scaffold (prompt → validate → `AnsiConsole.Progress()` → success/error → "Press Enter"). The dashboard menu dispatch at line 140–197 is clean, but the handlers it dispatches to shouldn't live here.

**Recommended fix:** Extract all `Handle*ArtifactAsync` methods into a new class `ArtifactActions.cs` (or separate per-action classes). Dashboard becomes a ~200-line orchestrator: show header, show registries, show menu, dispatch. The action classes receive `IServiceProvider` via constructor and own their prompt→execute→display cycle.

### 🟡 MEDIUM — Markup injection in registry table

**Line:** 92
```csharp
registryTable.AddRow(registry, status);
```
`registry` is a raw string from `DockerConfigStore.ListRegistriesAsync()`. If a Docker config contains a registry name with Spectre.Console markup characters (e.g., `[evil]` or brackets in hostnames), the `AddRow` call will misparse the markup. `status` is safe (hard-coded markup string).

**Recommended fix:** `registryTable.AddRow(Markup.Escape(registry), status);`

### 🟡 MEDIUM — Markup injection in Rule header (ManifestInspector, inherited pattern)

**Line (ManifestInspector.cs):** 34
```csharp
var header = new Rule($"[yellow]Manifest Inspector: {reference}[/]")
```
`reference` is user-provided text (e.g., `ghcr.io/org/repo:tag`). A reference containing `[` or `]` will break the Rule rendering.

**Recommended fix:** `$"[yellow]Manifest Inspector: {Markup.Escape(reference)}[/]"`

### 🟡 MEDIUM — `HandleLoginAsync` status spinner is fire-and-forget

**Lines:** 544–549
```csharp
AnsiConsole.Status()
    .Start("Validating credentials...", ctx =>
    {
        ctx.Spinner(Spinner.Known.Dots);
        ctx.SpinnerStyle(Style.Parse("green"));
    });
```
This spins up a status context, immediately exits (the lambda does nothing but set styles), then the actual `ValidateCredentialsAsync` runs *outside* the status context. The user sees a flash-and-gone spinner, then a blocking call with no visual feedback.

**Recommended fix:** Move the validation call inside the status lambda, matching the pattern used in `RegistryBrowser.VerifyRegistryConnectionAsync` (line 126–143).

### 🟡 MEDIUM — `DockerConfigStore` instantiated directly in constructors

**Lines:** 22 (Dashboard), 22 (RegistryBrowser)
Both classes do `_configStore = new DockerConfigStore()`. This bypasses any DI configuration and makes testing impossible without hitting the real Docker config file.

**Recommended fix:** Resolve `DockerConfigStore` from `IServiceProvider` or inject via constructor. At minimum, make it injectable so tests can substitute.

### 🔵 LOW — Duplicated "Press Enter to continue" boilerplate

**Lines:** 295, 409, 501, 572, 663, 760, 842 (and many more across all files)
Every handler ends with:
```csharp
AnsiConsole.WriteLine();
PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
```
This is repeated 15+ times across the codebase.

**Recommended fix:** Add `PromptHelper.PressEnterToContinue()` and call that everywhere.

### 🔵 LOW — Magic strings for menu options

**Lines:** 123–133, 144–187
Menu choices like `"Browse Registry"`, `"Push Artifact"`, etc. are string literals compared in a switch. If a label changes in one place but not the other, the action breaks silently.

**Recommended fix:** Use `const string` fields (already done in RegistryBrowser for `backOption`/`refreshOption`). Apply the same pattern in Dashboard.

---

## 2. RegistryBrowser.cs (948 lines)

### 🔴 CRITICAL — Largest file in codebase: 6+ distinct responsibilities

**What's wrong:** This file contains:
1. Registry selection/connection logic (lines 26–144)
2. Repository browsing with caching (lines 146–278)
3. Repository context menu + handlers (lines 280–460)
4. Tag browsing with caching (lines 462–612)
5. Tag context menu dispatch (lines 515–565)
6. Six full action handlers (pull/copy/backup/tag/delete for tags, lines 613–947)

The action handlers (pull, copy, backup, tag, delete) in this file are *near-identical copies* of the same handlers in Dashboard.cs and ManifestInspector.cs.

**Recommended fix:**
- Extract action handlers into shared `ArtifactActions.cs` (reusable from Dashboard, RegistryBrowser, and ManifestInspector)
- Extract repository browsing into `RepositoryBrowser.cs`
- RegistryBrowser becomes ~200 lines: connect → list repos → delegate to RepositoryBrowser

### 🟠 HIGH — Massive code duplication across all three files

The following operation patterns are copy-pasted 3 times (once in each of Dashboard, RegistryBrowser, ManifestInspector):
- **Pull workflow:** prompt → progress (resolve + download + write) → success
- **Copy workflow:** prompt → progress (resolve + copy layers + copy manifest) → success
- **Tag workflow:** prompt → progress (resolve + create tags) → success
- **Delete workflow:** confirm → progress (resolve + delete) → success
- **Backup workflow:** prompt → progress (fetch + download + write) → success

Each copy is 50–80 lines and nearly identical. Total duplicated code: ~600–800 lines.

**Recommended fix:** Create `TuiOperationRunner` with methods like `RunPullAsync(string reference)`, `RunCopyAsync(string source, string dest)`, etc. Each file calls the runner instead of reimplementing the workflow.

### 🟡 MEDIUM — `BrowseTagsAsync` is public but only for Dashboard cross-call

**Line:** 466
Made `public` solely so Dashboard can call it for "Browse Repository Tags". This leaks internal browsing state (credentials, cache) across class boundaries.

**Recommended fix:** Either make RegistryBrowser a shared service that Dashboard injects, or have Dashboard instantiate RegistryBrowser and call `RunAsync` with a pre-set registry/repository.

### 🔵 LOW — Cache TTL not configurable from outside

The `TuiCache` default TTL is 5 minutes. The `RegistryBrowser` constructor creates it with defaults. No way for the user or dashboard to configure cache behavior.

**Recommended fix:** Accept TTL configuration, or at minimum document the default. Low priority since 5 min is reasonable.

---

## 3. ManifestInspector.cs (670 lines)

### 🟠 HIGH — Action handlers are duplicated (same as Dashboard/RegistryBrowser)

**Lines:** 242–532
Five action handlers (`HandlePullAsync`, `HandleCopyAsync`, `HandleTagActionAsync`, `HandleDeleteActionAsync`) are copy-pasted from the other files with minor variations.

**Recommended fix:** Same as above — shared `ArtifactActions` class.

### 🟡 MEDIUM — JSON display uses raw `Markup` instead of `JsonText`

**Line:** 104
```csharp
var panel = new Panel(new Markup($"[dim]{Markup.Escape(manifest.Json)}[/]"))
```
The history notes say `JsonText` wasn't available in v0.50.0, but the code should check if the Spectre.Console version has been upgraded since. `JsonText` provides syntax highlighting (keys, values, strings in different colors) and proper wrapping. The current approach renders everything as dim monochrome text.

**Recommended fix:** Check current Spectre.Console version. If `JsonText` is now available, use it. Otherwise, implement a simple colorizing renderer for JSON keys/values/strings.

### 🟡 MEDIUM — Model classes are private nested classes

**Lines:** 644–669
`ManifestData`, `LayerData`, `ReferrerData` are `private class` nested inside ManifestInspector. These will need to be shared if/when real API integration happens and if the cache or other components need to reference them.

**Recommended fix:** Move to `src/Oras.Cli/Tui/Models/` as `internal` classes. This also enables sharing with TuiCache for typed caching.

### 🔵 LOW — `FormatSize` is a local utility

**Line:** 631–642
`FormatSize(long bytes)` is a useful general utility duplicated nowhere yet, but will be needed when real data flows in.

**Recommended fix:** Move to a shared `TuiFormatting` helper or extend `PromptHelper`.

---

## 4. PromptHelper.cs (123 lines)

### ⚪ INFORMATIONAL — Well-designed, consistent, safe

This is the best-structured file in the TUI layer. Key strengths:
- All message methods (`ShowError`, `ShowSuccess`, `ShowInfo`, `ShowWarning`) use `Markup.Escape` — safe by default
- Consistent API surface (`PromptText`, `PromptSecret`, `PromptSelection`, etc.)
- Good use of `SelectionPrompt.EnableSearch()` in the search variant

### 🟡 MEDIUM — Missing helpers that would reduce duplication

**Missing:**
1. `PressEnterToContinue()` — called 15+ times across all files as a 2-line snippet
2. `RunWithProgress(...)` — a generic progress-bar runner that accepts task definitions, eliminating the repeated `AnsiConsole.Progress().AutoClear(false).HideCompleted(false).Columns(...)` boilerplate
3. `ShowRule(string title)` — for consistent section headers with Rule widget

**Recommended fix:** Add these three helpers. The progress runner alone would eliminate ~200 lines of duplication.

### 🔵 LOW — `PromptSelection` and `PromptSelectionWithSearch` overlap

**Lines:** 36–71
These two methods are nearly identical. The only difference is `EnableSearch()` and a larger `PageSize`. Could be a single method with an `enableSearch` parameter (which `PromptSelectionWithSearch` already has — it's just never called with `false`).

**Recommended fix:** Merge into one method. `PromptSelection` becomes `PromptSelectionWithSearch(title, choices, converter, enableSearch: false)`.

---

## 5. TuiCache.cs (69 lines)

### 🟡 MEDIUM — Race condition in `Get<T>` between check and remove

**Lines:** 30–43
```csharp
if (_cache.TryGetValue(key, out var entry))
{
    if (DateTimeOffset.UtcNow < entry.ExpiresAt)
    {
        return ((T?)entry.Value, true, true);
    }
    else
    {
        _cache.TryRemove(key, out _);
    }
}
```
Between `TryGetValue` and `TryRemove`, another thread could have already set a new value for the same key. The `TryRemove` would delete the freshly-set value. In a single-threaded TUI this is unlikely but the class uses `ConcurrentDictionary`, implying thread-safety intent.

**Recommended fix:** Use `TryRemove` with the overload that checks the value: `_cache.TryRemove(new KeyValuePair<string, CacheEntry>(key, entry))` to only remove if the entry hasn't changed.

### 🟡 MEDIUM — `InvalidatePattern` uses `Contains` — no real pattern matching

**Lines:** 50–57
```csharp
var keys = _cache.Keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
```
Named `InvalidatePattern` but does substring matching, not pattern/glob matching. The naming is misleading.

**Recommended fix:** Rename to `InvalidateBySubstring` or `InvalidateContaining`. Or add actual glob/regex support.

### 🔵 LOW — No memory pressure management

The cache grows unboundedly within a session. For a typical TUI session this is fine (dozens of entries), but there's no max-size limit.

**Recommended fix:** Low priority. Add a max-entries check only if memory becomes a concern.

### ⚪ INFORMATIONAL — Good design overall

TTL-based expiration, `ConcurrentDictionary`, clear API. The tuple return `(T? Value, bool Found, bool FromCache)` is a nice pattern for cache-miss disambiguation.

---

## 6. Cross-Cutting Concerns

### 🟠 HIGH — Massive code duplication is the #1 issue

The single biggest problem across the TUI layer is that **pull, copy, backup, tag, and delete workflows are implemented 3 times** (Dashboard, RegistryBrowser, ManifestInspector). This is ~800 lines of near-identical code. Any bug fix or UX change must be applied in 3 places.

### 🟡 MEDIUM — Inconsistent markup escaping

Most places properly escape user input via `Markup.Escape()`. However:
- `Dashboard.cs:92` — `registryTable.AddRow(registry, status)` — `registry` unescaped
- `ManifestInspector.cs:34` — `reference` in Rule header unescaped
- `RegistryBrowser.cs:482` — `PromptHelper.ShowInfo($"No tags found for {repository}.")` — `repository` flows through `Markup.Escape` in `ShowInfo`, so this is **safe** (ShowInfo escapes internally)

Safe by design: All `PromptHelper.Show*` calls internally escape. The risk is in direct `AnsiConsole.*` and `AddRow`/`Rule`/`PanelHeader` calls with user data.

### 🟡 MEDIUM — No terminal width awareness

No code checks `Console.WindowWidth` or `AnsiConsole.Profile.Width`. Long registry names, repository paths, or manifest JSON will wrap unpredictably on narrow terminals. Spectre.Console's `Table` handles column widths automatically, but `Panel` content and `Markup` strings don't.

**Recommended fix:** For JSON panels, consider `Panel.Expand = true` and let Spectre.Console handle wrapping. For very long references, truncate with ellipsis in display contexts.

### 🔵 LOW — Color palette is consistent but undocumented

The color scheme is applied consistently:
- **Cyan1** — headers, panel borders, info icon
- **Green** — success, action prompts, selections
- **Yellow** — warnings, manifest headers, panel borders for inspector
- **Red** — errors, destructive operations
- **Grey/dim grey** — secondary info, cached indicators, metadata

This is good. Would benefit from a `TuiColors` static class to centralize constants and ensure future consistency.

### 🔵 LOW — `Console.Clear()` used directly instead of `AnsiConsole.Clear()`

**ManifestInspector.cs:** lines 31, 101, 117, 162
**Dashboard.cs:** line 61

`Console.Clear()` bypasses Spectre.Console's output pipeline. If output is being captured or redirected (testing), this won't clear properly. Use `AnsiConsole.Clear()` for consistency.

### ⚪ INFORMATIONAL — All operations use simulated `Task.Delay` (mock data)

Every fetch/action method uses `Task.Delay` as a placeholder. This is documented and expected for Sprint 3. When real API integration happens, the progress patterns will need real byte-count tracking instead of percentage simulation.

---

## Refactoring Plan

### Phase 1: Eliminate Duplication (Estimated: ~800 lines removed)

1. **Create `src/Oras.Cli/Tui/ArtifactActions.cs`** — Shared action implementations:
   - `RunPullAsync(string reference, CancellationToken ct)`
   - `RunCopyAsync(string source, string destination, bool includeReferrers, CancellationToken ct)`
   - `RunBackupAsync(string source, string outputPath, bool includeReferrers, CancellationToken ct)`
   - `RunRestoreAsync(string backupPath, string destination, CancellationToken ct)`
   - `RunTagAsync(string reference, string[] tags, CancellationToken ct)`
   - `RunDeleteAsync(string reference, ManifestData? manifest, CancellationToken ct)`

2. **Update Dashboard, RegistryBrowser, ManifestInspector** to delegate to `ArtifactActions` instead of implementing handlers inline.

### Phase 2: Extract and Restructure (Estimated: files drop to ~150–250 lines each)

3. **Move model classes** (`ManifestData`, `LayerData`, `ReferrerData`) to `src/Oras.Cli/Tui/Models/`.
4. **Split RegistryBrowser** into:
   - `RegistryBrowser.cs` — connection and registry selection (~100 lines)
   - `RepositoryBrowser.cs` — repo list, tag list, context menus (~200 lines)
5. **Extract dashboard action handlers** into `ArtifactActions.cs` (from Phase 1).

### Phase 3: Harden and Polish

6. **Add `PromptHelper.PressEnterToContinue()`** and **`PromptHelper.CreateProgressContext()`** helpers.
7. **Fix all markup injection points** (Dashboard:92, ManifestInspector:34).
8. **Fix `HandleLoginAsync` status spinner** to wrap the actual validation call.
9. **Create `TuiColors` static class** for centralized color constants.
10. **Replace `Console.Clear()` with `AnsiConsole.Clear()`** everywhere.
11. **Rename `TuiCache.InvalidatePattern` to `InvalidateContaining`**.

### Expected Outcome

| Metric | Before | After |
|--------|--------|-------|
| Dashboard.cs | 851 lines | ~200 lines |
| RegistryBrowser.cs | 948 lines | ~250 lines |
| ManifestInspector.cs | 670 lines | ~200 lines |
| Total TUI lines | ~2,592 | ~1,200 |
| Duplicated code | ~800 lines | 0 |
| New files | 0 | 3 (ArtifactActions, RepositoryBrowser, Models/) |

---

*Review complete. No blocking issues — the code works correctly. The primary concern is maintainability: the duplication will cause drift when real API integration begins.*
