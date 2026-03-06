# oras manifest fetch-config

Fetch the config blob of a manifest.

## Synopsis

```bash
oras manifest fetch-config <reference> [options]
```

## Description

The `manifest fetch-config` command fetches the configuration blob referenced by a manifest. The config blob contains metadata and settings for the artifact.

This is a convenience command that:
1. Fetches the manifest
2. Extracts the config descriptor
3. Fetches the config blob

## Arguments

### `<reference>`

Manifest reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `ghcr.io/myorg/app:v1.0`
- `ghcr.io/myorg/app@sha256:abc123...`

## Options

### Output Options

#### `--output <file>`, `-o`

Write config to file instead of stdout.

Example: `--output ./config.json`

#### `--descriptor`

Output the config descriptor (metadata) instead of config content.

#### `--pretty`

Pretty-print the JSON output with indentation.

### Platform Options

#### `--platform <os/arch>`

Select platform for multi-platform images (e.g., `linux/amd64`, `linux/arm64`).

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

### Fetch config to stdout

```bash
oras manifest fetch-config ghcr.io/myorg/app:v1.0
```

Output (compact JSON):
```json
{"architecture":"amd64","os":"linux","config":{...}}
```

### Fetch config with pretty printing

```bash
oras manifest fetch-config ghcr.io/myorg/app:v1.0 --pretty
```

Output (formatted JSON):
```json
{
  "architecture": "amd64",
  "os": "linux",
  "config": {
    "Env": ["PATH=/usr/local/bin:/usr/bin"],
    "Cmd": ["./app"]
  },
  "rootfs": {
    "type": "layers",
    "diff_ids": [
      "sha256:layer1...",
      "sha256:layer2..."
    ]
  }
}
```

### Fetch config to file

```bash
oras manifest fetch-config ghcr.io/myorg/app:v1.0 --output ./config.json
```

### Fetch config descriptor

```bash
oras manifest fetch-config ghcr.io/myorg/app:v1.0 --descriptor
```

Output:
```json
{
  "mediaType": "application/vnd.oci.image.config.v1+json",
  "digest": "sha256:config123...",
  "size": 1234
}
```

### Fetch specific platform config

```bash
oras manifest fetch-config ghcr.io/myorg/multiplatform:latest \
  --platform linux/arm64
```

### Fetch and pipe to jq

```bash
oras manifest fetch-config ghcr.io/myorg/app:v1.0 | jq '.architecture'
```

Output:
```
"amd64"
```

## Exit Codes

- `0` — Success (config written to output)
- `1` — Network error, registry error, manifest or config not found
- `2` — Usage error (invalid reference)

## Notes

- Config blob typically contains architecture, OS, and runtime configuration
- Use `--pretty` for human-readable output
- Use `--descriptor` to get metadata without downloading the config
- For multi-platform images, use `--platform` to select a specific variant
- Config content is usually JSON and can be piped to tools like `jq`

## Config Structure

OCI image configs typically contain:
- `architecture`: CPU architecture (e.g., `amd64`, `arm64`)
- `os`: Operating system (e.g., `linux`, `windows`)
- `config`: Runtime configuration (environment variables, entrypoint, etc.)
- `rootfs`: Root filesystem info (layer digests)
- `history`: Image layer history

## Manual Alternative

You can achieve the same result manually:

```bash
# Fetch manifest
MANIFEST=$(oras manifest fetch ghcr.io/myorg/app:v1.0)

# Extract config digest
CONFIG_DIGEST=$(echo "$MANIFEST" | jq -r '.config.digest')

# Fetch config blob
oras blob fetch ghcr.io/myorg/app@$CONFIG_DIGEST
```

The `manifest fetch-config` command does all of this in one step.

## See Also

- [manifest fetch](manifest-fetch.md) — Fetch the full manifest
- [blob fetch](blob-fetch.md) — Fetch blobs by digest
- [pull](pull.md) — Pull entire artifact
