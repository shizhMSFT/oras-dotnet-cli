# oras pull

Pull files from a remote registry.

## Synopsis

```bash
oras pull <reference> [options]
```

## Description

The `pull` command downloads all files (blobs) from an OCI artifact in a remote registry and saves them to the current directory or a specified output directory.

Files are named according to their annotations or blob digests if no name annotation is present.

## Arguments

### `<reference>`

Source reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `localhost:5000/myapp:latest`
- `ghcr.io/myorg/config:v1.0`
- `ghcr.io/myorg/artifact@sha256:abc123...`

## Options

### Output Options

#### `--output <dir>`, `-o`

Output directory for downloaded files (default: current directory).

Example: `--output ./downloads`

#### `--keep-old-files`

Do not overwrite existing files. Skip files that already exist in the output directory.

### Platform Options

#### `--platform <os/arch>`

Specify platform for multi-platform artifacts (e.g., `linux/amd64`, `linux/arm64`, `windows/amd64`).

Example: `--platform linux/arm64`

### Remote Options

#### `--username <username>`, `-u`

Registry username. If not provided, uses stored credentials.

#### `--password <password>`, `-p`

Registry password. If not provided, uses stored credentials.

**Warning:** Providing passwords on the command line may expose them to other users on the system.

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

#### `--concurrency <n>`

Number of concurrent downloads (default: `5`).

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Pull artifact to current directory

```bash
oras pull ghcr.io/myorg/config:v1
```

### Pull to specific directory

```bash
oras pull ghcr.io/myorg/config:v1 --output ./my-config
```

### Pull specific platform

```bash
oras pull ghcr.io/myorg/app:latest --platform linux/arm64
```

### Pull by digest

```bash
oras pull ghcr.io/myorg/artifact@sha256:abc123def456...
```

### Pull without overwriting

```bash
oras pull ghcr.io/myorg/config:v1 --keep-old-files
```

### Pull with authentication

```bash
oras pull ghcr.io/myorg/artifact:v1 \
  --username myuser \
  --password-stdin
```

### Pull with higher concurrency

```bash
oras pull ghcr.io/myorg/artifact:v1 --concurrency 10
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (invalid reference, permission denied)

## Notes

- Files are downloaded concurrently for better performance
- File names are determined from OCI annotations (typically `org.opencontainers.image.title`)
- If no name annotation is present, files are named by their digest
- Use `--platform` when pulling multi-platform artifacts to select a specific variant
- The manifest digest and artifact type are displayed on successful pull

## See Also

- [push](push.md) — Push files to a registry
- [copy](copy.md) — Copy artifacts between registries
- [manifest fetch](manifest-fetch.md) — Fetch only the manifest
