### 2026-03-06T08:27Z: User directive
**By:** Shiwei Zhang (via Copilot)
**What:** Some registries like ghcr.io do not support the catalog API for listing repositories. The TUI must handle this gracefully — when catalog listing fails or is unavailable, allow users to manually enter a repository name (e.g., `oras-project/oras`) and jump directly to tag listing. Always offer manual repository entry as an option alongside catalog results.
**Why:** User request — real-world registries vary in catalog API support. The TUI must not be a dead-end when catalog is unavailable.
