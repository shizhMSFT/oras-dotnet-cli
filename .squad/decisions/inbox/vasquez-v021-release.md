# v0.2.1 Patch Release Decision

**Date:** 2026-03-06
**Author:** Vasquez (DevOps)
**Status:** Executed

## Decision

Ship v0.2.1 as a patch release addressing 3 TUI and platform compatibility bugs discovered post-v0.2.0.

## Bug Fixes Included

1. **Spectre.Console markup crash** — ASCII-safe indicators (`[+]`, `[i]`, `[!]`, `[X]`) were interpreted as Spectre.Console markup tags, causing runtime crashes. Fixed by escaping to double-bracket form in PromptHelper.
2. **UTF-8 encoding** — Console output encoding now explicitly set to UTF-8 in Program.Main. Prevents garbled characters on Windows consoles that default to non-UTF-8 codepages.
3. **Banner alignment** — FigletText banner changed from centered to left-aligned for consistent rendering across terminal widths.

## Release Artifacts

- 6 platform binaries (win-x64, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64)
- GitHub Release auto-created via release.yml workflow
- GitHub Pages docs deployed via docs.yml workflow
- NuGet package skipped (no API key configured)

## Versioning Rationale

SemVer patch bump (0.2.0 → 0.2.1) — bug fixes only, fully backward compatible, no API or behavior changes beyond the fixes.

## Team Impact

- All download URLs in docs updated to v0.2.1
- Version verification output now shows 0.2.1
- No breaking changes — existing workflows unaffected
