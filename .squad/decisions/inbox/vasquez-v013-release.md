# Vasquez — v0.1.3 Release Complete

**Date:** 2026-03-06
**Status:** ✅ Complete
**Tag:** v0.1.3

## Summary

Shipped v0.1.3 with three critical new features: enhanced `oras copy`, new `oras backup`, and new `oras restore` commands, plus full TUI interactive workflows for all three. All documentation updated across 8 key files, version bumped, committed, tagged, and released.

## Work Completed

### Documentation
- ✅ Updated TUI Showcase with Dashboard v0.1.3 and new "Copy, Backup & Restore" section
- ✅ Enhanced TUI Guide with subsections for Copy, Backup, Restore workflows
- ✅ Updated Migration Guide: copy status ✅ Full, added backup/restore as 🆕 New
- ✅ Updated Index with v0.1.3 URLs and backup/restore Quick Start examples
- ✅ Updated Installation with 6 platform download URLs (v0.1.2 → v0.1.3) and version verification
- ✅ Updated Command Reference README with backup/restore entries marked "New — .NET CLI exclusive"
- ✅ Created docs/commands/backup.md with full reference documentation
- ✅ Created docs/commands/restore.md with full reference documentation

### Version & Release
- ✅ Bumped Directory.Build.props: 0.1.2 → 0.1.3
- ✅ Committed all changes with proper co-authored footer
- ✅ Pushed main branch
- ✅ Created and pushed v0.1.3 tag
- ✅ Release workflow succeeded (~4 min), created GitHub Release with 6 binaries
- ✅ Updated release notes with comprehensive markdown (features, download table, changelog link)

## Release Notes Highlights

**Features:**
- 📋 `oras copy` — Copy between registries with progress tracking, source auth, referrers support
- 💾 `oras backup` — Save artifacts to local OCI layout or tar archive (.NET CLI exclusive)
- 🔄 `oras restore` — Push local backups to registry (.NET CLI exclusive)
- 🖥️ TUI — Interactive workflows with prompts, progress bars, summary panels

**Platforms Included:**
- oras-win-x64.zip, oras-win-arm64.zip
- oras-linux-x64.tar.gz, oras-linux-arm64.tar.gz
- oras-osx-x64.tar.gz, oras-osx-arm64.tar.gz

## Next Steps for Team

1. **Dallas/Bishop**: Features (copy, backup, restore, TUI) are now documented and released
2. **Shiwei**: Release is live at https://github.com/shizhMSFT/oras-dotnet-cli/releases/tag/v0.1.3
3. **Team**: Consider these features in future planning:
   - Copy now fully functional (not a stub)
   - Backup/Restore are .NET CLI exclusive selling points
   - TUI now supports 3 artifact management workflows

## Files Modified

- Directory.Build.props (version bump)
- docs/tui-showcase.md (Dashboard v0.1.3, new section)
- docs/tui-guide.md (subsections added)
- docs/migration.md (copy status updated, backup/restore added)
- docs/index.md (URLs updated, examples added)
- docs/installation.md (6 URLs updated, version verification updated)
- docs/commands/README.md (backup/restore entries added)
- docs/commands/backup.md (created)
- docs/commands/restore.md (created)
- .squad/agents/vasquez/history.md (appended v0.1.3 learning)

## Release Artifacts

- GitHub Release: https://github.com/shizhMSFT/oras-dotnet-cli/releases/tag/v0.1.3
- Commit: d949ebf (feat: oras copy, backup, restore — commands + TUI + docs)
- Tag: v0.1.3
- Binaries: All 6 platforms available for download
