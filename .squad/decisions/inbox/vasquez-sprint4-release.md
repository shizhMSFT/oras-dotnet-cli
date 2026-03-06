# Sprint 4 Release Infrastructure Decisions

**Date**: 2026-03-06  
**Author**: Vasquez (DevOps)  
**Context**: Sprint 4 items S4-01, S4-02, S4-08 - Native AOT, Release Pipeline, GitHub Pages

## Decision 1: Native AOT Configuration Strategy

**Decision**: Configure AOT at project level with publish profiles per RID, not via Directory.Build.props

**Rationale**:
- Project-level configuration keeps AOT settings isolated to the CLI app (not test projects)
- Publish profiles provide RID-specific overrides without complex MSBuild conditions
- Easier to maintain and understand than conditional properties in Directory.Build.props

**Configuration**:
```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>true</InvariantGlobalization>
<PublishTrimmed>true</PublishTrimmed>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<TrimMode>link</TrimMode>
<IlcOptimizationPreference>Speed</IlcOptimizationPreference>
```

**Trade-offs**:
- ✅ Clear separation between CLI and test projects
- ✅ Publish profiles are self-documenting for each platform
- ❌ Settings duplicated across 6 publish profiles (acceptable - profiles are simple)

## Decision 2: Trimmer Preservation Strategy

**Decision**: Use `TrimmerRoots.xml` descriptor file + `<TrimmerRootAssembly>` elements for trimming control

**Rationale**:
- Spectre.Console.Json namespace was being trimmed, causing compilation errors
- TrimmerRoots.xml provides fine-grained control (namespace-level preservation)
- TrimmerRootAssembly provides coarse-grained safety net (entire assemblies)
- Both approaches together ensure AOT compatibility

**Preserved Types**:
- `Spectre.Console.Json` namespace (used by ManifestInspector and TextFormatter)
- Entire assemblies: Spectre.Console, System.CommandLine, OrasProject.Oras

**Known Warning**:
- `IL3050` on `AnsiConsole.WriteException()` — Spectre.Console's exception formatter uses reflection
- Decision: Accept warning (non-critical, only affects error display formatting)
- Alternative would be to write custom exception formatter (not worth complexity)

## Decision 3: Release Pipeline Architecture

**Decision**: Multi-job GitHub Actions workflow with build matrix and separate NuGet job

**Rationale**:
- Build matrix parallelizes RID builds across appropriate runners (Windows for Windows RIDs, etc.)
- Separate NuGet job prevents accidental pre-release tool publishing
- GitHub-hosted runners provide clean environments and all target platforms

**Workflow Structure**:
1. **Build job**: Matrix across 6 RIDs → publish → compress → upload artifacts
2. **Release job**: Download all artifacts → create GitHub Release with changelog
3. **NuGet job**: Pack as `dotnet tool` → push to NuGet.org (only for stable releases)

**Trade-offs**:
- ✅ Parallel builds reduce total release time
- ✅ Separate jobs allow partial success (e.g., NuGet can fail without affecting binaries)
- ❌ More complex workflow (acceptable for production releases)

**Compression Strategy**:
- Windows: `.zip` (standard for Windows users)
- Unix: `.tar.gz` (preserves execute permissions, better compression)

## Decision 4: GitHub Pages Documentation Approach

**Decision**: Use Jekyll (GitHub's native generator) with Cayman theme, not docfx

**Rationale**:
- Jekyll is GitHub Pages' native generator (no build step required, automatic deployment)
- Cayman theme is clean, professional, and works well with technical documentation
- Simpler than docfx (no .NET build, no separate CI step for docs)
- Markdown-first approach matches existing docs structure

**Site Structure**:
```
docs/
├── index.md           # Homepage
├── installation.md    # Installation guide (existing)
├── tui-guide.md      # TUI guide (new)
├── commands/         # Command reference (existing)
└── _config.yml       # Jekyll configuration
```

**Workflow**:
- Trigger: Push to `main` with `docs/**` changes
- Uses `actions/jekyll-build-pages@v1` for consistency with GitHub's Jekyll environment
- Deploys via `actions/deploy-pages@v4` with proper permissions

**Trade-offs**:
- ✅ Zero-config deployment (GitHub handles Jekyll automatically)
- ✅ Fast iteration (commit to main → deployed in ~2 minutes)
- ❌ Limited customization compared to docfx (acceptable for MVP)

## Decision 5: NuGet Tool Publishing

**Decision**: Make NuGet publishing optional and conditional on non-pre-release tags

**Rationale**:
- Not all users will configure `NUGET_API_KEY` secret
- Pre-release tags (e.g., `v1.0.0-beta`) should not publish to NuGet.org
- Binary releases are primary distribution method; `dotnet tool` is secondary

**Conditions**:
- Only runs if `NUGET_API_KEY` secret is set
- Skipped for pre-release tags (tags containing `-`)
- Fails silently if secret is missing (doesn't block release)

**Tool Configuration**:
```bash
dotnet pack -p:PackAsTool=true -p:PackageId=oras -p:ToolCommandName=oras
```

## Testing & Verification

All decisions verified with:
- ✅ `dotnet build -c Release` passes
- ✅ `dotnet publish -c Release -r win-x64` succeeds with AOT
- ✅ Binary size: ~10.5 MB (single-file, self-contained)
- ✅ Only acceptable warning: IL3050 (Spectre.Console reflection in error handler)

## Future Considerations

1. **AOT Compatibility**: If more IL3050 warnings appear, consider:
   - Suppressing specific warnings with `<NoWarn>IL3050</NoWarn>`
   - Replacing reflection-heavy Spectre.Console features with AOT-compatible alternatives

2. **Binary Size Optimization**: Current 10.5 MB is acceptable, but could be reduced:
   - Investigate removing unused Spectre.Console features
   - Profile which assemblies contribute most to size

3. **Documentation Enhancement**:
   - Consider docfx if richer API docs needed
   - Add search functionality (GitHub Pages supports Lunr.js)
   - Add version dropdown for multi-version docs

4. **Release Automation**:
   - Add automated changelog generation from conventional commits
   - Integrate with GitHub Releases API for richer release notes
   - Consider auto-tagging from version bumps
