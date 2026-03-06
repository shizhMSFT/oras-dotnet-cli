---
title: manifest push
layout: default
parent: Command Reference
nav_order: 13
---

# oras manifest push

Push a manifest to a registry.

## Synopsis

```bash
oras manifest push <reference> <file> [options]
```

## Description

The `manifest push` command uploads a pre-built OCI manifest file to a registry and tags it.

This is a low-level operation for pushing manually created manifests or manifests obtained from other sources.

## Arguments

### `<reference>`

Target reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `ghcr.io/myorg/app:v1.0` (push with tag)
- `ghcr.io/myorg/app` (push without tag)

### `<file>`

Path to manifest file (JSON format following OCI manifest schema).

## Options

### Manifest Options

#### `--media-type <type>`

Media type for the manifest. If not specified, uses the `mediaType` field from the manifest file.

Common values:
- `application/vnd.oci.image.manifest.v1+json`
- `application/vnd.oci.image.index.v1+json`
- `application/vnd.docker.distribution.manifest.v2+json`

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

### Push manifest with tag

```bash
oras manifest push ghcr.io/myorg/app:v1.0 ./manifest.json
```

Output:
```
Pushed sha256:abc123...
```

### Push manifest without tag

```bash
oras manifest push ghcr.io/myorg/app ./manifest.json
```

The manifest is pushed but not tagged. You can tag it later using `oras tag`.

### Push with specific media type

```bash
oras manifest push ghcr.io/myorg/app:v1.0 ./manifest.json \
  --media-type application/vnd.oci.image.manifest.v1+json
```

### Push with authentication

```bash
oras manifest push ghcr.io/myorg/app:v1.0 ./manifest.json \
  --username myuser \
  --password-stdin
```

### Push and capture digest

```bash
DIGEST=$(oras manifest push ghcr.io/myorg/app:v1.0 ./manifest.json)
echo "Manifest digest: $DIGEST"
```

## Exit Codes

- `0` — Success (digest printed to stdout)
- `1` — Network error, registry error, upload failure
- `2` — Usage error (file not found, invalid JSON, invalid reference)

## Notes

- Manifest file must be valid JSON following OCI manifest schema
- All blobs referenced by the manifest must exist in the registry before pushing
- Use `blob push` to upload blobs before pushing the manifest
- The manifest digest (computed from content) is printed on success
- Media type can be specified in the manifest file or via `--media-type` option

## Manifest File Format

Example OCI manifest (`manifest.json`):

```json
{
  "schemaVersion": 2,
  "mediaType": "application/vnd.oci.image.manifest.v1+json",
  "config": {
    "mediaType": "application/vnd.oci.image.config.v1+json",
    "digest": "sha256:config123...",
    "size": 1234
  },
  "layers": [
    {
      "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip",
      "digest": "sha256:layer123...",
      "size": 5678,
      "annotations": {
        "org.opencontainers.image.title": "config.yaml"
      }
    }
  ],
  "annotations": {
    "org.opencontainers.image.created": "2024-01-01T00:00:00Z"
  }
}
```

## Typical Workflow

1. **Push blobs first:**
   ```bash
   CONFIG_DIGEST=$(oras blob push ghcr.io/myorg/app ./config.json)
   LAYER_DIGEST=$(oras blob push ghcr.io/myorg/app ./layer.tar.gz)
   ```

2. **Create manifest referencing blobs:**
   ```bash
   # Create manifest.json with $CONFIG_DIGEST and $LAYER_DIGEST
   cat > manifest.json <<EOF
   {
     "schemaVersion": 2,
     "mediaType": "application/vnd.oci.image.manifest.v1+json",
     "config": {
       "mediaType": "application/vnd.oci.image.config.v1+json",
       "digest": "$CONFIG_DIGEST",
       "size": 1234
     },
     "layers": [
       {
         "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip",
         "digest": "$LAYER_DIGEST",
         "size": 5678
       }
     ]
   }
   EOF
   ```

3. **Push manifest:**
   ```bash
   oras manifest push ghcr.io/myorg/app:v1.0 ./manifest.json
   ```

## See Also

- [manifest fetch](manifest-fetch.md) — Fetch a manifest from a registry
- [manifest delete](manifest-delete.md) — Delete a manifest
- [blob push](blob-push.md) — Push blobs
- [push](push.md) — Push complete artifact (blobs + manifest automatically)
