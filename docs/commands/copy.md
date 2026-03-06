# oras copy

Copy artifacts between registries or repositories.

## Synopsis

```bash
oras copy <source> <destination> [options]
```

## Description

The `copy` command copies an OCI artifact from one registry or repository to another. This includes the manifest and all referenced blobs.

With the `--recursive` flag, the command also copies all referrers (artifacts that reference the source artifact), enabling full artifact graph copying.

## Arguments

### `<source>`

Source reference in the format `[registry/]repository[:tag|@digest]`.

Example: `ghcr.io/source/artifact:v1.0`

### `<destination>`

Destination reference in the format `[registry/]repository[:tag|@digest]`.

Example: `ghcr.io/dest/artifact:v1.0`

## Options

### Copy Options

#### `--recursive`, `-r`

Recursively copy all referrers (artifacts attached to the source artifact).

This copies the entire artifact graph, including signatures, attestations, and other attached artifacts.

### Platform Options

#### `--platform <os/arch>`

Specify platform for multi-platform artifacts (e.g., `linux/amd64`, `linux/arm64`).

When copying multi-platform images, this selects a specific platform to copy.

### Remote Options

#### `--username <username>`, `-u`

Registry username (applies to both source and destination if same).

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

### Performance Options

#### `--concurrency <n>`

Number of concurrent copy operations (default: `5`).

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Copy artifact within same registry

```bash
oras copy ghcr.io/source/artifact:v1.0 ghcr.io/dest/artifact:v1.0
```

### Copy between different registries

```bash
oras copy ghcr.io/source/artifact:v1.0 docker.io/dest/artifact:v1.0
```

### Copy with all referrers (signatures, attestations)

```bash
oras copy ghcr.io/source/artifact:v1.0 ghcr.io/dest/artifact:v1.0 --recursive
```

### Copy specific platform

```bash
oras copy ghcr.io/source/multiplatform:latest ghcr.io/dest/arm64:latest \
  --platform linux/arm64
```

### Copy by digest

```bash
oras copy \
  ghcr.io/source/artifact@sha256:abc123... \
  ghcr.io/dest/artifact:stable
```

### Copy with higher concurrency

```bash
oras copy ghcr.io/source/artifact:v1.0 ghcr.io/dest/artifact:v1.0 \
  --concurrency 10
```

### Copy between registries with different credentials

```bash
# Login to both registries first
oras login ghcr.io
oras login docker.io

# Then copy
oras copy ghcr.io/source/artifact:v1.0 docker.io/dest/artifact:v1.0
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (invalid reference, authentication failure)

## Notes

- Copy is efficient: blobs that already exist at the destination are not re-uploaded
- Use `--recursive` to copy entire artifact graphs (useful for signed images)
- Credentials are read from the Docker configuration file for both source and destination
- Multi-platform images are copied as-is unless `--platform` is specified
- The copied manifest gets a new timestamp but the digest is preserved

## See Also

- [push](push.md) — Push artifacts to a registry
- [pull](pull.md) — Pull artifacts from a registry
- [tag](tag.md) — Tag an artifact
- [attach](attach.md) — Attach artifacts to create referrers
