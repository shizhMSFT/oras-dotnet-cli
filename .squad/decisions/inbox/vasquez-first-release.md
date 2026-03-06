# Decision: Versioning Scheme for oras .NET CLI

**Author:** Vasquez (DevOps)  
**Date:** 2026-03-06  
**Status:** Proposed

## Context

We need a versioning scheme for the oras .NET CLI that communicates maturity, integrates with .NET tooling, and drives release automation (pre-release detection, NuGet gating).

## Decision

Use **SemVer 2.0** with the following conventions:

- **Pre-release tags**: `v{major}.{minor}.{patch}-{label}.{n}` (e.g., `v0.1.0-alpha.1`, `v0.2.0-beta.1`, `v1.0.0-rc.1`)
- **Stable releases**: `v{major}.{minor}.{patch}` (e.g., `v1.0.0`)
- **Version source of truth**: `<Version>` property in `Directory.Build.props` — applies to all projects uniformly
- **Git tag format**: `v`-prefixed to trigger release workflow (`v*` pattern in release.yml)

### Automation implications

| Tag contains `-` | Pre-release flag | NuGet publish | Example            |
|-------------------|-----------------|---------------|--------------------|
| Yes               | true            | Skipped       | `v0.1.0-alpha.1`  |
| No                | false           | Runs          | `v1.0.0`           |

### Alpha release specifics

For alpha releases, AOT compilation and IL trimming are **disabled** at publish time (`-p:PublishAot=false -p:PublishTrimmed=false`) to maximize runtime compatibility. These will be re-enabled for beta/RC once trim compatibility is validated.

## Consequences

- Version must be bumped in `Directory.Build.props` before tagging each release
- The `v` prefix is mandatory for tags — bare version numbers won't trigger the pipeline
- Pre-release labels control downstream behavior automatically; no manual flags needed
