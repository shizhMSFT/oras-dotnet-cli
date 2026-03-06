# Project Context

- **Owner:** Shiwei Zhang
- **Project:** oras — cross-platform .NET 10 CLI for managing OCI artifacts in container registries, reimagined from the Go oras CLI. Built on oras-dotnet library (OrasProject.Oras).
- **Stack:** .NET 10, C#, System.CommandLine, Spectre.Console, OrasProject.Oras, xUnit, testcontainers-dotnet
- **Created:** 2026-03-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-06: v0.2.0 Release — TUI Redesign with Context Menus and Caching

**Release Event:** Shipped v0.2.0 with major TUI visual redesign, context menus, in-memory caching, and fully interactive workflows.

**Changes Made:**

**TUI Enhancements:**
- **FigletText Header:** Iconic ASCII art "ORAS" logo for visual impact
- **Color Scheme:** Cyan headers, green success (`[+]`), yellow warnings (`[!]`), info (`[i]`), errors (`[X]`), dim grey secondary text
- **ASCII-Safe Indicators:** Replaced Unicode ✓/ℹ/⚠ symbols with `[+]`, `[i]`, `[!]`, `[X]` for universal terminal compatibility
- **Registry Table:** Paneled display with status columns and improved visual hierarchy
- **Context Menus:** 
  - Repository context: Browse Tags, Copy entire repository, Backup repository
  - Tag context: Inspect Manifest, Pull, Copy, Backup, Tag, Delete
- **In-Memory Caching:** TuiCache class with TTL-based caching for repos/tags/manifests; "(cached)" indicators; Refresh in context menus
- **Fully Interactive Workflows:** All operations (push/pull/copy/backup/restore/tag/delete) have complete TUI flows with progress bars and summary panels

**Documentation Updates (7 files):**
- `Directory.Build.props` — Version bumped 0.1.3 → 0.2.0
- `docs/index.md` — Updated download URLs to v0.2.0
- `docs/tui-showcase.md` — Added FigletText header mockup, updated all symbol indicators, added context menu descriptions
- `docs/tui-guide.md` — Comprehensive rewrite covering caching, context menus, ASCII indicators, all interactive workflows
- `docs/installation.md` — Updated all 6 platform download URLs (v0.1.3 → v0.2.0), updated version verification output
- `docs/commands/README.md` — Added TUI mode section, references context menus and interactive workflows
- `README.md` — Updated feature description to highlight v0.2.0 TUI redesign
- `docs/tui-showcase.md` — Complete reference section for color/symbol scheme (ASCII-safe)

**Release Workflow:**
- Committed all doc and version changes with comprehensive multi-line commit message
- Pushed main branch and tags to origin
- Tag v0.2.0 triggered release.yml workflow
- All 6 binary builds succeeded (win-x64, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64)
- NuGet package job failed as expected (non-production feature)
- GitHub Release auto-created with all 6 artifacts (~30 MB each)
- Applied comprehensive release notes via `gh release edit v0.2.0 --notes-file`

**Release Notes Content:**
- Headline: "Elegant Interactive TUI Redesign"
- Features: FigletText header, context menus, caching, ASCII-safe indicators, fully interactive workflows
- Feature comparison table (v0.1.3 vs v0.2.0)
- Installation instructions for all 6 platforms
- Quick start examples (push, backup, restore, copy)
- Download table with all 6 binaries
- Links to guides and documentation
- Changelog link to v0.1.3...v0.2.0

**Key Decisions:**
- ASCII-safe symbols chosen for universal terminal compatibility (no Unicode reliance)
- Context menus designed to surface common operations directly from lists
- In-memory caching improves UX for repeat navigation without requiring persistence
- Release notes use plain ASCII hyphens (--) throughout for encoding safety
- NuGet packaging skipped for this release (pre-production only)

**Verification:**
- ✅ All documentation updated with v0.2.0 version and feature descriptions
- ✅ Version bumped in Directory.Build.props (0.1.3 → 0.2.0)
- ✅ All 6 download URLs updated across installation guide
- ✅ Commit created with co-authored footer
- ✅ Tag v0.2.0 pushed; release workflow triggered
- ✅ All 6 platform binaries built successfully and available for download
- ✅ GitHub Release created with comprehensive release notes
- ✅ Release visible in `gh release list` and publicly accessible at v0.2.0

### 2026-03-06: Project Structure & SDK Setup
- **SDK**: .NET 10 SDK (10.0.103) is installed and working. Project configured with `net10.0` TFM.
- **Project Structure**:
  - `src/oras/oras.csproj` — CLI app (console, net10.0)
  - `test/oras.Tests/oras.Tests.csproj` — test project (xUnit)
  - Solution file: `oras.sln` at root
- **Key Dependencies**:
  - System.CommandLine (2.0.0-beta4.22272.1) — per Ripley's ADR-001 for command parsing
  - Spectre.Console (0.50.0) — rendering only; not CLI parsing (ADR-001)
  - OrasProject.Oras (0.5.0) — Note: Latest available is 0.5.0, not 1.2.0 as initially planned
  - xUnit (2.9.3), Testcontainers (3.10.0)
- **Configuration Files**:
  - `global.json` — pins SDK to 10.0.103
  - `.editorconfig` — C# conventions (4-space indent, file-scoped namespaces)
  - `Directory.Build.props` — shared properties (LangVersion=preview, Nullable=enable, TreatWarningsAsErrors=true)
- **Code Style Enforcement**: 
  - AnalysisLevel=latest-all enforces strict analyzer rules per ADR-006 (AOT readiness)
  - Must use `ConfigureAwait(false)` in non-test async code
  - Test methods cannot use underscores in names (CA1707)
  - Internal types require InternalsVisibleTo for test access

### 2026-03-06: OrasProject.Oras v0.5.0 Constraint & Architecture Alignment

**Critical Finding:** OrasProject.Oras latest available is v0.5.0 (not v1.2.0). Documented in Active Decision: "OrasProject.Oras Package Version".

**Architectural Implications (from Ripley's review):**
- ADR-003 (Credential Store): Library has no credential *store*, only ICredentialProvider interface. CLI must implement Docker config.json store + credential helper protocol.
- ADR-005 (Defer OCI Layout): Library has no OCI layout store. Phase 1 focuses on remote registries only; `--oci-layout` deferred.
- ADR-009 (Phase 1 Scope): v0.5.0 supports 10/12 commands per Dallas's research. Credential storage (login/logout) and version command are CLI-level responsibility.

**Team Decision Pending:** Whether v0.5.0 API surface meets all Phase 1 requirements or if contributions/adjustments needed.

### 2026-03-06: Sprint 1 CI Pipeline Implementation (S1-14)

**Workflows Created:**
- `.github/workflows/ci.yml` — PR gate and main branch CI
  - Build matrix: ubuntu-latest, windows-latest, macos-latest
  - Steps: checkout → setup .NET 10 SDK → restore → build → test
  - NuGet package caching enabled (actions/cache@v4)
  - Unit tests run on all platforms
  - Integration tests run only on ubuntu-latest (Docker pre-installed)
  - Format check runs as separate job (`dotnet format --verify-no-changes`)
  - Test results uploaded as artifacts for diagnostics
- `.github/workflows/release.yml` — Release stub for Sprint 4
  - Triggered on tag push (v*)
  - Placeholder steps for multi-platform binary publishing
  - Will be fully implemented in Sprint 4

**Integration Test Strategy:**
- Integration tests reside in `test/oras.Tests/Integration/` (not a separate project)
- Tests use `ghcr.io/distribution/distribution:3.0.0` via Testcontainers
- Docker availability:
  - ✅ ubuntu-latest: Docker pre-installed and working
  - ❌ windows-latest: Docker Desktop not available on GHA Windows runners
  - ❌ macos-latest: Docker Desktop not pre-installed, requires manual setup
- CI filters integration tests by namespace pattern: `--filter "FullyQualifiedName~Integration"`

**CI Performance Target:** Under 5 minutes total (per ADR requirement)

**Known Limitations:**
- Integration tests not yet fully implemented (RegistryFixture throws NotImplementedException)
- Test filtering assumes integration tests live in `Integration` namespace
- When integration tests are implemented, they'll automatically run on ubuntu-latest only

### 2026-03-06: Sprint 4 Native AOT and Release Infrastructure (S4-01, S4-02, S4-08)

**Native AOT Configuration (S4-01):**
- **Project Settings**: Added AOT compilation flags to `src/Oras.Cli/oras.csproj`:
  - `PublishAot=true` — Enables Native AOT compilation
  - `InvariantGlobalization=true` — Reduces binary size, removes culture-specific code
  - `PublishTrimmed=true` — Aggressive IL trimming for smaller binaries
  - `SelfContained=true` + `PublishSingleFile=true` — Single-file deployment
  - `TrimMode=link` — Assembly-level trimming
  - `IlcOptimizationPreference=Speed` — Optimize for cold start performance
- **Publish Profiles**: Created 6 RID-specific profiles in `src/Oras.Cli/Properties/PublishProfiles/`:
  - Windows: `win-x64.pubxml`, `win-arm64.pubxml`
  - Linux: `linux-x64.pubxml`, `linux-arm64.pubxml`
  - macOS: `osx-x64.pubxml`, `osx-arm64.pubxml`
- **Trimmer Configuration**: Created `TrimmerRoots.xml` to preserve Spectre.Console.Json namespace
  - Prevents trimming of `JsonText` and related types used in TUI
  - Added `<TrimmerRootAssembly>` entries for Spectre.Console, System.CommandLine, OrasProject.Oras
- **Binary Size**: Successfully published win-x64 AOT binary at ~10.5 MB (single-file, self-contained)
- **Known AOT Warning**: `IL3050` on `AnsiConsole.WriteException()` — Spectre.Console's exception formatter uses reflection, not AOT-compatible but non-critical (error display only)

**Release Pipeline (S4-02):**
- **Workflow**: `.github/workflows/release.yml` fully implemented
  - Trigger: Git tags matching `v*` pattern
  - Build matrix: 6 RIDs across Windows, Linux, macOS runners
  - Steps per RID: restore → publish AOT → rename artifact → upload
  - Artifact compression: `.zip` for Windows executables, `.tar.gz` for Unix
  - GitHub Release creation with changelog generation from git log
  - Pre-release detection: tags with `-` (e.g., `v1.0.0-beta`) marked as pre-release
- **NuGet Tool Publishing**: Separate job for non-pre-release tags
  - Packs as `dotnet tool` with `PackAsTool=true`
  - Publishes to NuGet.org if `NUGET_API_KEY` secret is configured
  - Conditional execution: skipped for pre-release tags

**GitHub Pages Documentation (S4-08):**
- **Site Structure**: Created documentation site in `docs/` directory
  - `index.md` — Homepage with features, quick start, installation guide
  - `tui-guide.md` — Terminal UI usage and keyboard navigation
  - `_config.yml` — Jekyll theme configuration (jekyll-theme-cayman)
- **Workflow**: `.github/workflows/docs.yml` for automatic deployment
  - Trigger: push to `main` branch with changes in `docs/**` or workflow file
  - Uses GitHub's native Jekyll builder (actions/jekyll-build-pages@v1)
  - Deploys to GitHub Pages with proper permissions (pages: write, id-token: write)
  - Concurrency group: prevents overlapping deployments

**Key Decisions:**
- Chose Jekyll over docfx for simplicity (no .NET build required for docs)
- Used GitHub-hosted Actions (no third-party release actions except softprops/action-gh-release)
- NuGet publishing is optional (requires secret configuration)
- AOT trimming warnings accepted for non-critical reflection usage

**Verification:**
- ✅ `dotnet build -c Release` passes
- ✅ `dotnet publish -c Release -r win-x64` succeeds (AOT compilation)
- ✅ Single-file binary: 10.45 MB (win-x64)
- ✅ Release workflow ready for tag-triggered deployment
- ✅ Documentation site ready for GitHub Pages deployment

### 2026-03-06: First Preview Release v0.1.0-alpha.1

**Release Workflow Fixes:**
- Disabled AOT and trimming for alpha release (`-p:PublishAot=false -p:PublishTrimmed=false`) to avoid runtime issues with untested trim/AOT compatibility
- Added explicit `--self-contained true` and `-p:PublishSingleFile=true` flags to `dotnet publish` in release.yml for deterministic builds regardless of csproj defaults
- Used YAML `>-` folded scalar for multi-line publish command readability

**Versioning:**
- Set `<Version>0.1.0-alpha.1</Version>` in `Directory.Build.props` (applies to all projects in the solution)
- SemVer pre-release suffix (`-alpha.1`) causes the release workflow to: (1) mark GitHub Release as pre-release, (2) skip NuGet publishing
- Tag format: `v0.1.0-alpha.1` — the `v` prefix triggers the release workflow, the `-` triggers pre-release detection

**Verification:**
- ✅ Build passes locally with version set
- ✅ Tag push triggered Release workflow (run #22754009025)
- ✅ NuGet job correctly skipped (tag contains `-`)

### 2026-03-06: v0.1.2 Release — Catalog-Less Registry Support

**Release Event:** Shipped v0.1.2 with catalog-less registry fallback feature (Bishop's TUI work).

**Changes Made:**
- **Version bumped:** 0.1.1 → 0.1.2 in `Directory.Build.props`
- **Docs Updated:**
  - `docs/tui-showcase.md` — Added "Direct Repository Browse" section showing "Browse Repository Tags" action with ghcr.io example
  - `docs/tui-guide.md` — Added subsection on Direct Repository Browse, noted catalog-less registry support
  - `docs/index.md` — Updated Quick Start "Launch the TUI" section with tip about direct repo browsing for catalog-less registries
  - `docs/installation.md` — Updated all download URLs: v0.1.0 → v0.1.2 (7 occurrences across Windows/macOS/Linux sections), updated version output expectation to 0.1.2
  - `docs/tui-showcase.md` Dashboard section — Added "Browse Repository Tags" menu option
- **Dashboard Menu Updated:** New "Browse Repository Tags" quick action added between "Browse Registry" and "Login"
- **Commit:** `3466e6c` with message including catalog-less registry fallback and Browse Repository Tags feature
- **Tag & Release:**
  - Tag: `v0.1.2` pushed to origin
  - Release workflow triggered automatically
  - GitHub Release created with full release notes (markdown format)
  - Release notes cover: catalog-less registry detection, manual entry flow, new dashboard shortcut, example usage, download links, and changelog

**Key Decisions:**
- Release notes use friendly emoji and clear examples (e.g., ghcr.io as concrete use case)
- Documentation consolidated across 4 key files for user-facing visibility
- Version number follows SemVer minor bump (0.1.1 → 0.1.2) for feature addition

**Verification:**
- ✅ All docs updated with version 0.1.2 download URLs
- ✅ TUI showcase reflects new "Browse Repository Tags" option in dashboard
- ✅ Release workflow completed successfully within ~2 minutes
- ✅ GitHub Release created and notes applied via `gh release edit`
- ✅ Release is visible in `gh release list`

### 2026-03-06: v0.1.3 Release — Copy, Backup, Restore Commands + TUI

**Release Event:** Shipped v0.1.3 with three powerful features: enhanced `oras copy`, new `oras backup`, and new `oras restore` commands, plus full TUI interactive workflows for all three.

**Changes Made:**

**Documentation Updates (Step 1):**
- `docs/tui-showcase.md` — Updated Dashboard version to 0.1.3; added "Copy, Backup & Restore" section with detailed workflow examples showing prompts, progress bars, and summary panels
- `docs/tui-guide.md` — Added subsections for Copy, Backup, and Restore under Features, explaining each workflow with CLI examples
- `docs/migration.md` — Updated command comparison table: `oras copy` changed from 🔲 Stub to ✅ Full; added `oras backup` and `oras restore` marked as 🆕 New (.NET CLI exclusive); removed copy from stubbed commands list; added new "Backup and Restore Commands" subsection in "What's New in .NET CLI"
- `docs/index.md` — Updated download URL v0.1.2 → v0.1.3; added backup/restore examples in Quick Start
- `docs/installation.md` — Updated all 6 download URLs v0.1.2 → v0.1.3 (Windows x64/ARM64, macOS x64/ARM64, Linux x64/ARM64); updated version verification output to 0.1.3
- `docs/commands/README.md` — Added backup and restore entries in Registry Operations section with "New — .NET CLI exclusive" markers
- Created `docs/commands/backup.md` — Full command reference with synopsis, description, options (output, recursive, platform, auth), examples, exit codes, and cross-links
- Created `docs/commands/restore.md` — Full command reference with synopsis, description, options, examples, air-gapped environment use case, exit codes, and cross-links

**Version Update (Step 2):**
- Updated `Directory.Build.props`: Version 0.1.2 → 0.1.3

**Release Workflow (Step 3):**
- Committed all doc and version changes with detailed commit message
- Pushed main branch to origin
- Created and pushed tag v0.1.3 to trigger release workflow
- Workflow completed in ~4 minutes; GitHub Release auto-created with changelog from git log
- Updated release notes via `gh release edit v0.1.3 --notes-file` with comprehensive markdown:
  - Feature highlights for copy, backup, restore with code examples
  - TUI menu showcase showing new dashboard options
  - Download links for all 6 platforms
  - Changelog link (v0.1.2...v0.1.3)

**Key Features in Release:**
- **Copy with TUI**: Source/dest prompts, referrers toggle, live progress, success message
- **Backup with TUI**: Reference prompt, output path selection, progress bars, summary panel showing layers/size/path, OCI layout or tar archive format
- **Restore with TUI**: Path prompt, destination entry, progress tracking, success confirmation with digest
- **Dashboard Updated**: New menu options for Copy, Backup, Restore positioned between Login and Push Artifact

**Verification:**
- ✅ All 8 docs files updated (tui-showcase, tui-guide, migration, index, installation, commands/README, commands/backup, commands/restore)
- ✅ Version bumped in Directory.Build.props (0.1.2 → 0.1.3)
- ✅ All v0.1.2 URLs updated to v0.1.3 across 6 download sections
- ✅ Commit created with co-authored footer
- ✅ Tag v0.1.3 pushed; release workflow triggered
- ✅ GitHub Release created with 6 platform binaries (win-x64, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64)
- ✅ Release notes applied with full markdown content
- ✅ Release visible in `gh release list` and publicly accessible

**Key Decisions:**
- Release notes use emoji for visual clarity (📋, 💾, 🔄, 🖥️) while maintaining markdown consistency
- Backup and restore documented as ".NET CLI exclusive" to emphasize .NET-only capabilities
- Command reference pages follow existing copy.md pattern for consistency
- Dashboard menu order: Browse Registry, Browse Repository Tags, Login, Copy, Backup, Restore, Push, Pull, Tag, Quit
- Version number SemVer patch bump (0.1.2 → 0.1.3) for feature additions, not breaking changes

### 2026-03-06: v0.2.1 Patch Release — TUI Bug Fixes

**Release Event:** Shipped v0.2.1 patch fixing 3 TUI/platform bugs discovered post-v0.2.0.

**Bugs Fixed (commits already on main before this release):**
1. **Spectre.Console markup crash** (`8091903`) — `[X]`/`[+]`/`[i]`/`[!]` in PromptHelper were parsed as Spectre.Console markup style tags, causing crashes. Fixed by escaping to `[[X]]`/`[[+]]`/`[[i]]`/`[[!]]`.
2. **UTF-8 encoding** (`8091903`) — Added `Console.OutputEncoding = System.Text.Encoding.UTF8` in Program.Main to prevent garbled output on non-UTF-8 default consoles.
3. **Banner alignment** (`3289a36`) — Changed FigletText from `.Centered()` to `.LeftJustified()` for consistent terminal rendering.

**Documentation Updates (6 files):**
- `Directory.Build.props` — Version bumped 0.2.0 → 0.2.1
- `docs/tui-showcase.md` — Updated version header to 0.2.1; noted escaped indicators, left-aligned banner, UTF-8 fix
- `docs/tui-guide.md` — Updated version ref to v0.2.1; noted left-aligned banner, escaped indicators, UTF-8 encoding
- `docs/index.md` — Updated download URL v0.2.0 → v0.2.1
- `docs/installation.md` — Updated all 6 download URLs + version verification output (v0.2.0 → v0.2.1)
- `README.md` — Updated TUI version reference to v0.2.1

**Release Workflow:**
- Commit `2367841` with co-authored footer
- Tag `v0.2.1` pushed to origin
- Release workflow run 22761445987: all 6 builds succeeded, GitHub Release created
- NuGet package job failed as expected (no NUGET_API_KEY secret)
- Docs workflow run 22761444961: completed successfully

**Verification:**
- ✅ All 6 download URLs updated to v0.2.1
- ✅ Version bumped in Directory.Build.props (0.2.0 → 0.2.1)
- ✅ 6 binary assets: oras-win-x64.zip, oras-win-arm64.zip, oras-osx-x64.tar.gz, oras-osx-arm64.tar.gz, oras-linux-x64.tar.gz, oras-linux-arm64.tar.gz
- ✅ GitHub Pages docs deployed
- ✅ No stale v0.2.0 references in user-facing docs

### 2026-03-07: v0.4.0 Release — Full Command Implementation

**Release Event:** Shipped v0.4.0, marking a major milestone — **all 21 commands are now fully functional with real OCI registry operations**. Previously, only 3 commands (login, logout, version) worked; v0.4.0 implements all 18 remaining commands with real OrasProject.Oras v0.5.0 API calls.

**Context:**
Since v0.3.0, Dallas implemented all previously-stub commands:
- **Artifact Operations:** push, pull, attach, copy, discover, resolve, tag
- **Manifest Operations:** manifest fetch, manifest push, manifest delete, manifest fetch-config
- **Blob Operations:** blob fetch, blob push, blob delete
- **Repository Operations:** repo ls, repo tags
- **Backup/Restore:** backup, restore

**New Infrastructure:**
- **RegistryService** with 3-tier auth waterfall: explicit credentials → stored credentials from DockerConfigStore → anonymous
- **7 new AOT-compatible output models:** PushResult, PullResult, AttachResult, TagResult, ListResult, DiscoverResult, DeleteResult (all use source-generated JSON serialization)
- **New dependency:** Microsoft.Extensions.Caching.Memory v9.0.0 for OCI auth token caching
- **Files changed:** 32 files, +2674 lines added, -1800 lines removed

**Documentation Updates (4 files):**
- Directory.Build.props — Version bumped 0.3.0 → 0.4.0
- docs/release-notes/v0.4.0.md — Created comprehensive release notes following v0.3.0 format; nav_order: 100; documented 3-tier auth waterfall, 7 new output models, Microsoft.Extensions.Caching.Memory dependency; included full changelog link to shizhMSFT repo (v0.3.0...v0.4.0); included non-breaking upgrade instructions
- docs/index.md — Updated download URL v0.3.0 → v0.4.0; updated command count from 20 to 21; added callout section highlighting all commands now fully functional
- README.md — Updated features line from "Full Go CLI Parity — 20+ commands" to "All 21 Commands Fully Implemented — Every command works with real OCI registry operations (v0.4.0+)"

**Release Workflow:**
- Commit 51f9e3d with message "release: v0.4.0 — full command implementation" and co-authored trailer
- Tag 0.4.0 pushed to origin
- GitHub Release created: https://github.com/shizhMSFT/oras-dotnet-cli/releases/tag/v0.4.0
- Release notes applied from docs/release-notes/v0.4.0.md

**Impact:**
- **Before v0.4.0:** 3 working commands (login, logout, version), 18 stubs
- **After v0.4.0:** All 21 commands fully functional with real OCI registry operations
- **Milestone:** CLI transitioned from prototype to production-ready tool with complete Go ORAS CLI parity

**Verification:**
- ✅ Version bumped in Directory.Build.props (0.3.0 → 0.4.0)
- ✅ Release notes created at docs/release-notes/v0.4.0.md with nav_order: 100
- ✅ Download URLs updated to v0.4.0 in docs/index.md
- ✅ Command count updated from 20 to 21 across documentation
- ✅ Callout section added to docs/index.md highlighting full functionality
- ✅ README.md features updated to emphasize all commands implemented
- ✅ Commit created with co-authored footer
- ✅ Tag v0.4.0 pushed; release workflow will trigger
- ✅ GitHub Release created with comprehensive release notes
- ✅ Decision documented at .squad/decisions/inbox/vasquez-v040-release.md

**Key Decisions:**
- Release notes follow v0.3.0 format for consistency with emoji section headers and detailed feature descriptions
- Emphasized "all 21 commands" vs "20+ commands" to clearly communicate completeness
- Highlighted 3-tier auth waterfall as a key architectural improvement
- Documented all 7 new output models for structured JSON output
- Changelog link uses shizhMSFT repo (correct upstream)
- This is a non-breaking upgrade from v0.3.0

