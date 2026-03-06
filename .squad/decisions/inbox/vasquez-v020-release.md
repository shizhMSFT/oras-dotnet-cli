# v0.2.0 Release Decision: TUI Redesign with Context Menus and Caching

## Decision

Ship v0.2.0 with comprehensive TUI visual redesign, context menus, in-memory caching, and fully interactive workflows.

## Rationale

The TUI redesign significantly improves user experience through:
1. **Visual Polish:** FigletText header and rich color scheme create professional appearance
2. **Discoverability:** Context menus surface all relevant actions without requiring CLI knowledge
3. **Performance:** In-memory caching with TTL makes repeat navigation dramatically faster
4. **Completeness:** All operations now have full interactive flows with progress and summary panels
5. **Compatibility:** ASCII-safe indicators work on all terminal types without Unicode fallback

## Changes

### TUI Enhancements
- FigletText ASCII art "ORAS" header
- Cyan/green/yellow/grey color scheme
- ASCII-safe status indicators: `[+]` (success), `[i]` (info), `[!]` (warning), `[X]` (error)
- Paneled registry display with status columns
- Repository context menu: Browse Tags, Copy, Backup
- Tag context menu: Inspect, Pull, Copy, Backup, Tag, Delete
- TuiCache class with TTL-based caching and Refresh options
- Fully interactive workflows for all operations

### Documentation
- Updated 7 doc files with v0.2.0 version and new features
- FigletText header mockup in tui-showcase.md
- Context menus documented in tui-guide.md and tui-showcase.md
- Caching behavior explained with refresh instructions
- All 6 platform download URLs updated
- Comprehensive release notes with feature comparison table

### Version
- Directory.Build.props: 0.1.3 → 0.2.0

## Release Artifacts

All 6 platform binaries successfully built and available:
- oras-win-x64.zip (31.25 MiB)
- oras-win-arm64.zip (30.01 MiB)
- oras-osx-x64.tar.gz (31.32 MiB)
- oras-osx-arm64.tar.gz (29.56 MiB)
- oras-linux-x64.tar.gz (31.17 MiB)
- oras-linux-arm64.tar.gz (29.55 MiB)

NuGet packaging skipped (pre-production feature).

## Backward Compatibility

✅ **Fully backward compatible** — All v0.1.3 commands and workflows continue to work unchanged. ASCII-safe indicators are purely a UI enhancement with no functional impact.

## Next Steps

- Monitor for issues in interactive workflows and caching behavior
- Gather user feedback on TUI redesign and context menu usability
- Consider Homebrew formula for macOS installation in future release
