# Decision: Catalog API Fallback — Manual Repository Entry

**Author:** Bishop (TUI Dev)
**Date:** 2026-03-06
**Status:** Implemented

## Context

Many public registries (ghcr.io, Docker Hub partial, ECR) don't support the `/v2/_catalog` endpoint. The TUI's "Browse Registry" flow was a dead-end when catalog failed — users saw "No repositories found" and could only go back.

## Decision

1. **Null vs empty semantics in `FetchRepositoriesAsync`:** `null` signals "catalog not supported"; empty `List<string>` signals "catalog worked but no repos exist." This drives different info messages to the user.

2. **"Enter repository name..." always present:** Whether catalog succeeds or fails, users can manually enter a repo path. This is appended to the bottom of every repo selection list.

3. **Graceful degradation on errors:** Unexpected fetch exceptions are treated as catalog-unavailable (return null) rather than blocking the user. They can still type a repo name.

4. **Dashboard shortcut — "Browse Repository Tags":** Lets users jump directly to tag browsing by entering a full reference like `ghcr.io/oras-project/oras`. Parses registry from the first `/` segment.

5. **`BrowseTagsAsync` made public:** So Dashboard can reuse it directly without duplicating tag-browsing logic.

## Impact

- **Dallas (CLI core):** No impact. TUI-only changes.
- **Mercer (tests):** New public method `BrowseTagsAsync` on `RegistryBrowser` is testable. Consider integration tests for catalog-fallback paths once real API is wired.
- **Rook (services):** When implementing real catalog API calls, throw `NotSupportedException` for registries that don't support `/v2/_catalog` — the TUI catches it and falls back gracefully.
