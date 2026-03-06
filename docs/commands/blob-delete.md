---
title: blob delete
layout: default
parent: Command Reference
nav_order: 18
---

# oras blob delete

Delete a blob from a registry.

## Synopsis

```bash
oras blob delete <reference> [options]
```

## Description

The `blob delete` command removes a blob from a registry by its digest.

**Warning:** This is a destructive operation. Deleting a blob that is referenced by manifests will break those manifests.

## Arguments

### `<reference>`

Blob reference in the format `[registry/]repository@<digest>`.

**Note:** Must use digest (`@sha256:...`), not tag.

Example: `ghcr.io/myorg/app@sha256:abc123...`

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

### Delete a blob (with confirmation)

```bash
oras blob delete ghcr.io/myorg/app@sha256:abc123...
```

Output:
```
Are you sure you want to delete blob sha256:abc123...? [y/N]: y
Deleted sha256:abc123...
```

### Delete without confirmation

```bash
oras blob delete ghcr.io/myorg/app@sha256:abc123... --force
```

### Delete with authentication

```bash
oras blob delete ghcr.io/myorg/app@sha256:abc123... \
  --force \
  --username myuser \
  --password-stdin
```

## Exit Codes

- `0` — Success (blob deleted)
- `1` — Network error, registry error, blob not found, permission denied
- `2` — Usage error (invalid reference, tag used instead of digest)

## Notes

- **Destructive operation**: Deletion is permanent and cannot be undone
- Blob reference must use digest (`@sha256:...`), not tag
- Without `--force`, prompts for confirmation
- Deleting a blob referenced by manifests will break those manifests
- Some registries may not support blob deletion (check registry capabilities)
- Deleted blobs may still be accessible for a short time due to caching

## Safety Considerations

Before deleting a blob:

1. **Check manifest references:**
   ```bash
   # Fetch manifests that might reference the blob
   oras manifest fetch ghcr.io/myorg/app:v1.0
   ```

2. **Ensure no manifests reference it:**
   Deleting a blob that's still referenced will cause pull failures for those manifests.

3. **Use digest, not tag:**
   Tags point to manifests, not blobs. You need the blob digest.

4. **Consider garbage collection:**
   Some registries prefer to use garbage collection to clean up unreferenced blobs automatically rather than manual deletion.

## When to Delete Blobs

- **After replacing content:** Old blob no longer referenced by any manifest
- **Cleaning up failed uploads:** Orphaned blobs from incomplete operations
- **Reducing storage costs:** Removing large, unused blobs
- **Before re-uploading:** Deleting corrupted blobs

## Registry Support

Not all registries support blob deletion:
- ✅ GitHub Container Registry (ghcr.io)
- ✅ Azure Container Registry
- ✅ Harbor
- ⚠️ Docker Hub (limited support)
- ⚠️ Some registries may require admin privileges

## See Also

- [blob fetch](blob-fetch.md) — Fetch a blob from a registry
- [blob push](blob-push.md) — Push a blob to a registry
- [manifest delete](manifest-delete.md) — Delete a manifest
