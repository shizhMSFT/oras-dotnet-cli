---
title: resolve
layout: default
parent: Command Reference
nav_order: 9
---

# oras resolve

Resolve a reference to a manifest digest.

## Synopsis

```bash
oras resolve <reference> [options]
```

## Description

The `resolve` command resolves a reference (tag or digest) to its canonical manifest digest (SHA256).

This is useful for:
- Getting the immutable digest for a tag
- Verifying tag resolution
- Pinning to specific versions in CI/CD pipelines

## Arguments

### `<reference>`

Reference to resolve in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `ghcr.io/myorg/artifact:latest`
- `ghcr.io/myorg/artifact@sha256:abc123...` (returns the same digest)

## Options

### Platform Options

#### `--platform <os/arch>`

Specify platform for multi-platform artifacts (e.g., `linux/amd64`, `linux/arm64`).

When resolving an image index (multi-platform image), this selects a specific platform manifest.

Example: `--platform linux/arm64`

### Remote Options

#### `--username <username>`, `-u`

Registry username.

#### `--password <password>`, `-p`

Registry password.

#### `--password-stdin`

Read password from stdin.

#### `--insecure`

Skip TLS certificate verification.

#### `--plain-http`

Use HTTP instead of HTTPS.

#### `--ca-file <file>`

Path to custom CA certificate.

#### `--registry-config <file>`

Path to registry configuration file (default: `~/.docker/config.json`).

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Resolve a tag

```bash
oras resolve ghcr.io/myorg/artifact:latest
```

Output:
```
sha256:abc123def456...
```

### Resolve with platform selection

```bash
oras resolve ghcr.io/myorg/multiplatform:latest --platform linux/arm64
```

### Resolve by digest (returns same digest)

```bash
oras resolve ghcr.io/myorg/artifact@sha256:abc123...
```

Output:
```
sha256:abc123...
```

### Resolve with authentication

```bash
oras resolve ghcr.io/myorg/artifact:latest \
  --username myuser \
  --password-stdin
```

### Use in scripts to pin versions

```bash
# Get digest and use in pull
DIGEST=$(oras resolve ghcr.io/myorg/artifact:latest)
oras pull ghcr.io/myorg/artifact@$DIGEST
```

## Exit Codes

- `0` — Success (digest printed to stdout)
- `1` — Network error, registry error, reference not found
- `2` — Usage error (invalid reference)

## Notes

- Output is the digest only (no additional formatting)
- For multi-platform images without `--platform`, the index digest is returned
- Use with `--platform` to resolve a specific platform manifest within an index
- Resolving by digest simply returns the same digest (validates it exists)

## See Also

- [tag](tag.md) — Tag a manifest
- [manifest fetch](manifest-fetch.md) — Fetch the full manifest
- [pull](pull.md) — Pull artifact contents
