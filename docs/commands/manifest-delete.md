---
title: manifest delete
layout: default
parent: Command Reference
nav_order: 14
---

# oras manifest delete

Delete a manifest from a registry.

## Synopsis

```bash
oras manifest delete <reference> [options]
```

## Description

The `manifest delete` command removes a manifest from a registry. This is typically done by deleting the tag or digest reference.

**Warning:** This is a destructive operation. Deleting a manifest by digest is permanent and affects all tags pointing to it.

## Arguments

### `<reference>`

Manifest reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `ghcr.io/myorg/app:v1.0` (delete tag)
- `ghcr.io/myorg/app@sha256:abc123...` (delete by digest)

**Important:** Deleting by tag removes only the tag. Deleting by digest removes the manifest and affects all tags pointing to it.

## Options

### Confirmation Options

#### `--force`, `-f`

Skip confirmation prompt. Use for non-interactive/scripted deletions.

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

### Delete a tag (with confirmation)

```bash
oras manifest delete ghcr.io/myorg/app:v1.0
```

Output:
```
Are you sure you want to delete manifest ghcr.io/myorg/app:v1.0? [y/N]: y
Deleted ghcr.io/myorg/app:v1.0
```

### Delete without confirmation

```bash
oras manifest delete ghcr.io/myorg/app:v1.0 --force
```

### Delete by digest

```bash
oras manifest delete ghcr.io/myorg/app@sha256:abc123... --force
```

### Delete with authentication

```bash
oras manifest delete ghcr.io/myorg/app:v1.0 \
  --force \
  --username myuser \
  --password-stdin
```

## Exit Codes

- `0` — Success (manifest deleted)
- `1` — Network error, registry error, manifest not found, permission denied
- `2` — Usage error (invalid reference)

## Notes

- **Destructive operation**: Deletion is permanent and cannot be undone
- Without `--force`, prompts for confirmation
- Deleting by tag removes only the tag reference
- Deleting by digest removes the manifest and affects all tags
- Some registries may not support manifest deletion
- Deleted manifests may be accessible for a short time due to caching

## Tag vs. Digest Deletion

### Delete by Tag
```bash
oras manifest delete ghcr.io/myorg/app:v1.0 --force
```
- Removes only the `v1.0` tag
- Manifest is still accessible by digest or other tags
- Other tags pointing to the same manifest remain

### Delete by Digest
```bash
oras manifest delete ghcr.io/myorg/app@sha256:abc123... --force
```
- Removes the manifest entirely
- All tags pointing to this digest are affected
- The manifest becomes inaccessible

## Safety Considerations

Before deleting a manifest:

1. **Check other tags:**
   ```bash
   # List all tags
   oras repo tags ghcr.io/myorg/app
   
   # Resolve tag to digest
   oras resolve ghcr.io/myorg/app:v1.0
   ```

2. **Check for referrers:**
   ```bash
   # Check if other artifacts reference this manifest
   oras discover ghcr.io/myorg/app:v1.0
   ```

3. **Consider using tags:**
   If you need to "delete" a version, consider deleting only the tag rather than the digest.

## When to Delete Manifests

- **Removing old versions:** Delete outdated tags
- **Fixing mistakes:** Remove incorrectly pushed manifests
- **Cleaning up tests:** Remove temporary test artifacts
- **Compliance:** Remove manifests that violate policies
- **Reducing storage:** Remove unused manifests (check referrers first)

## Registry Support

Not all registries support manifest deletion:
- ✅ GitHub Container Registry (ghcr.io)
- ✅ Azure Container Registry
- ✅ Harbor
- ⚠️ Docker Hub (requires special permissions)
- ⚠️ Some registries may require admin privileges

## See Also

- [manifest fetch](manifest-fetch.md) — Fetch a manifest
- [manifest push](manifest-push.md) — Push a manifest
- [tag](tag.md) — Create tags
- [blob delete](blob-delete.md) — Delete blobs
