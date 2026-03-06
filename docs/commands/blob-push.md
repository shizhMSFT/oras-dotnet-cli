---
title: blob push
layout: default
parent: Command Reference
nav_order: 17
---

# oras blob push

Push a blob to a registry.

## Synopsis

```bash
oras blob push <reference> <file> [options]
```

## Description

The `blob push` command uploads a single file as a blob to a registry.

This is a low-level operation that pushes content without creating a manifest. The blob can be referenced by manifests created later.

## Arguments

### `<reference>`

Target repository in the format `[registry/]repository`.

**Note:** Do not include a tag or digest; blobs are addressed by their computed digest.

Example: `ghcr.io/myorg/app`

### `<file>`

File to push as a blob.

## Options

### Blob Options

#### `--media-type <type>`

Media type for the blob. If not specified, defaults to `application/octet-stream`.

Example: `--media-type application/vnd.oci.image.layer.v1.tar+gzip`

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

### Push a blob

```bash
oras blob push ghcr.io/myorg/app ./layer.tar.gz
```

Output:
```
Pushed sha256:abc123...
```

### Push with specific media type

```bash
oras blob push ghcr.io/myorg/app ./layer.tar.gz \
  --media-type application/vnd.oci.image.layer.v1.tar+gzip
```

### Push with authentication

```bash
oras blob push ghcr.io/myorg/app ./config.json \
  --media-type application/vnd.oci.image.config.v1+json \
  --username myuser \
  --password-stdin
```

### Push and capture digest

```bash
DIGEST=$(oras blob push ghcr.io/myorg/app ./layer.tar.gz)
echo "Blob digest: $DIGEST"
```

## Exit Codes

- `0` — Success (digest printed to stdout)
- `1` — Network error, registry error, upload failure
- `2` — Usage error (file not found, invalid reference)

## Notes

- Blob digest is computed from file content (SHA256)
- The computed digest is printed to stdout on success
- Blobs are content-addressable; identical content produces the same digest
- Use `manifest push` to create a manifest that references the blob
- Pushed blobs are not garbage collected until referenced by a manifest
- If the blob already exists (same digest), it is not re-uploaded

## Typical Workflow

1. **Push blobs:**
   ```bash
   BLOB1=$(oras blob push ghcr.io/myorg/app ./file1.txt)
   BLOB2=$(oras blob push ghcr.io/myorg/app ./file2.txt)
   ```

2. **Create manifest referencing blobs:**
   ```bash
   # Create manifest.json with $BLOB1 and $BLOB2 in layers array
   oras manifest push ghcr.io/myorg/app:v1 ./manifest.json
   ```

3. **Pull the artifact:**
   ```bash
   oras pull ghcr.io/myorg/app:v1
   ```

## Common Media Types

| Media Type | Description |
|------------|-------------|
| `application/vnd.oci.image.layer.v1.tar+gzip` | Compressed tar layer |
| `application/vnd.oci.image.layer.v1.tar` | Uncompressed tar layer |
| `application/vnd.oci.image.config.v1+json` | Image configuration |
| `application/octet-stream` | Generic binary data (default) |
| `text/plain; charset=utf-8` | Plain text |
| `application/json` | JSON data |

## See Also

- [blob fetch](blob-fetch.md) — Fetch a blob from a registry
- [blob delete](blob-delete.md) — Delete a blob from a registry
- [manifest push](manifest-push.md) — Push a manifest
- [push](push.md) — Push complete artifact (blobs + manifest)
