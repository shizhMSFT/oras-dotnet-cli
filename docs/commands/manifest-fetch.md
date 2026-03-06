# oras manifest fetch

Fetch a manifest from a registry.

## Synopsis

```bash
oras manifest fetch <reference> [options]
```

## Description

The `manifest fetch` command downloads an OCI manifest from a registry. The manifest contains metadata about the artifact, including layer digests, annotations, and artifact type.

## Arguments

### `<reference>`

Manifest reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `ghcr.io/myorg/app:v1.0` (by tag)
- `ghcr.io/myorg/app@sha256:abc123...` (by digest)

## Options

### Output Options

#### `--output <file>`, `-o`

Write manifest to file instead of stdout.

Example: `--output ./manifest.json`

#### `--descriptor`

Output the manifest descriptor (metadata) instead of manifest content.

The descriptor includes media type, size, digest, and platform information.

#### `--pretty`

Pretty-print the JSON output with indentation.

By default, manifests are printed as compact JSON.

### Platform Options

#### `--platform <os/arch>`

Select platform for multi-platform images (e.g., `linux/amd64`, `linux/arm64`).

When fetching an image index (multi-platform image), this returns the platform-specific manifest.

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

### Fetch manifest to stdout

```bash
oras manifest fetch ghcr.io/myorg/app:v1.0
```

Output (compact JSON):
```json
{"schemaVersion":2,"mediaType":"application/vnd.oci.image.manifest.v1+json","config":{...},"layers":[...]}
```

### Fetch manifest with pretty printing

```bash
oras manifest fetch ghcr.io/myorg/app:v1.0 --pretty
```

Output (formatted JSON):
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
      "size": 5678
    }
  ]
}
```

### Fetch manifest to file

```bash
oras manifest fetch ghcr.io/myorg/app:v1.0 --output ./manifest.json
```

### Fetch manifest descriptor

```bash
oras manifest fetch ghcr.io/myorg/app:v1.0 --descriptor
```

Output:
```json
{
  "mediaType": "application/vnd.oci.image.manifest.v1+json",
  "digest": "sha256:abc123...",
  "size": 1234
}
```

### Fetch specific platform manifest

```bash
oras manifest fetch ghcr.io/myorg/multiplatform:latest --platform linux/arm64
```

### Fetch by digest

```bash
oras manifest fetch ghcr.io/myorg/app@sha256:abc123... --pretty
```

### Fetch and pipe to jq

```bash
oras manifest fetch ghcr.io/myorg/app:v1.0 | jq '.layers[].digest'
```

Output:
```
"sha256:layer1..."
"sha256:layer2..."
"sha256:layer3..."
```

## Exit Codes

- `0` — Success (manifest written to output)
- `1` — Network error, registry error, manifest not found
- `2` — Usage error (invalid reference)

## Notes

- Use `--pretty` for human-readable output
- Use `--descriptor` to get metadata without downloading the full manifest
- For multi-platform images, use `--platform` to select a specific variant
- Manifest content is JSON and can be piped to tools like `jq` for processing
- Without `--output`, writes to stdout (redirect to save to file)

## Manifest Structure

OCI manifests typically contain:
- `schemaVersion`: Manifest schema version (usually 2)
- `mediaType`: Manifest media type
- `config`: Reference to configuration blob
- `layers`: Array of layer descriptors (blobs)
- `annotations`: Optional key-value metadata

## See Also

- [manifest push](manifest-push.md) — Push a manifest to a registry
- [manifest delete](manifest-delete.md) — Delete a manifest
- [manifest fetch-config](manifest-fetch-config.md) — Fetch the config blob
- [blob fetch](blob-fetch.md) — Fetch individual blobs
