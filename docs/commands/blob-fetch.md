---
title: blob fetch
layout: default
parent: Command Reference
nav_order: 16
---

# oras blob fetch

Fetch a blob from a registry.

## Synopsis

```bash
oras blob fetch <reference> [options]
```

## Description

The `blob fetch` command downloads a blob (layer) from a registry by its digest.

Blobs are the raw content stored in OCI registries. This command is useful for inspecting or extracting individual layers without pulling the entire artifact.

## Arguments

### `<reference>`

Blob reference in the format `[registry/]repository@<digest>`.

**Note:** Must use digest (`@sha256:...`), not tag.

Example: `ghcr.io/myorg/app@sha256:abc123...`

## Options

### Output Options

#### `--output <file>`, `-o`

Output file path. If not specified, writes to stdout.

Example: `--output ./blob.tar.gz`

#### `--descriptor`

Output the blob descriptor (JSON) instead of blob content.

The descriptor includes media type, size, and digest information.

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

### Fetch blob to file

```bash
oras blob fetch ghcr.io/myorg/app@sha256:abc123... --output ./layer.tar.gz
```

### Fetch blob to stdout

```bash
oras blob fetch ghcr.io/myorg/app@sha256:abc123... | tar -tzf -
```

### Fetch blob descriptor

```bash
oras blob fetch ghcr.io/myorg/app@sha256:abc123... --descriptor
```

Output:
```json
{
  "mediaType": "application/vnd.oci.image.layer.v1.tar+gzip",
  "digest": "sha256:abc123...",
  "size": 1234567
}
```

### Fetch with authentication

```bash
oras blob fetch ghcr.io/myorg/app@sha256:abc123... \
  --output ./blob.tar.gz \
  --username myuser \
  --password-stdin
```

### Fetch and pipe to another command

```bash
oras blob fetch ghcr.io/myorg/app@sha256:abc123... | gunzip | tar -x
```

## Exit Codes

- `0` — Success (blob written to output)
- `1` — Network error, registry error, blob not found
- `2` — Usage error (invalid reference, tag used instead of digest)

## Notes

- Blob reference must use digest (`@sha256:...`), not tag
- Use `--output` to save to file; omit to write to stdout
- Use `--descriptor` to get metadata instead of content
- Blob content is streamed (no intermediate storage)
- To find blob digests, use `manifest fetch` to inspect the manifest

## Finding Blob Digests

To get blob digests from a manifest:

```bash
# Fetch manifest
oras manifest fetch ghcr.io/myorg/app:v1.0

# Look for "layers" array with digest values
# Then fetch individual blobs
oras blob fetch ghcr.io/myorg/app@sha256:abc123... --output layer1.tar.gz
```

## See Also

- [blob push](blob-push.md) — Push a blob to a registry
- [blob delete](blob-delete.md) — Delete a blob from a registry
- [manifest fetch](manifest-fetch.md) — Fetch manifest to find blob digests
- [pull](pull.md) — Pull entire artifact (all blobs)
