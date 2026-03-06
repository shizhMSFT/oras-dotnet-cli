# Release v0.1.2 — Catalog-Less Registry Support

**Date:** 2026-03-06 (UTC)  
**Released by:** Vasquez (DevOps)  
**Trigger:** Bishop's TUI feature (catalog-less registry fallback)  

## Summary

Released v0.1.2 with catalog-less registry support. Registries like ghcr.io, ECR, and Docker Hub private repos don't expose the catalog API (`/v2/_catalog`). The TUI now gracefully handles this:

1. **Detects unavailable catalog** — Shows: *"This registry does not support repository listing (e.g., ghcr.io)"*
2. **Always offers manual entry** — Select "Enter repository name..." to type a repo path directly
3. **New dashboard shortcut** — "Browse Repository Tags" action for quick direct access to any repo+tag on catalog-less registries

## Files Changed

| File | Changes |
|------|---------|
| `Directory.Build.props` | Version: 0.1.1 → 0.1.2 |
| `docs/tui-showcase.md` | Added "Direct Repository Browse" section, updated Dashboard menu |
| `docs/tui-guide.md` | Added subsection on Direct Repository Browse, noted catalog-less support |
| `docs/index.md` | Updated Quick Start tip about Browse Repository Tags |
| `docs/installation.md` | Updated all 7 download URLs to v0.1.2, fixed version output example |

## Release Workflow

- **Commit:** `3466e6c` pushed to `main`
- **Tag:** `v0.1.2` pushed to origin
- **Workflow:** Release.yml triggered, completed in ~2 minutes
- **Release Notes:** Applied via `gh release edit` with full feature description, examples, and download table
- **Status:** ✅ Complete — visible on GitHub Releases page

## Why This Matters

Users who rely on ghcr.io (GitHub Container Registry), ECR, or private Docker registries without public catalog APIs now have a seamless path to browse tags directly. No more "No repositories found" dead end.

## Next Steps

- Monitor for user feedback on the new feature
- If issues arise, v0.1.3 can add refinements (e.g., search-while-typing for direct repo entry)
- Docs are now aligned with v0.1.2 across all platforms

## Release Notes Link

https://github.com/shizhMSFT/oras-dotnet-cli/releases/tag/v0.1.2
