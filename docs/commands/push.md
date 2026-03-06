# oras push

Push files to a remote registry as an OCI artifact.

## Synopsis

```bash
oras push <reference> [<file>...] [options]
```

## Description

The `push` command packages one or more files into an OCI artifact and pushes it to a remote registry. Files are stored as blobs and referenced by a manifest.

Each file is pushed as a separate layer with its media type inferred from the file extension or explicitly set via options.

## Arguments

### `<reference>`

Target reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `localhost:5000/myapp:latest`
- `ghcr.io/myorg/config:v1.0`

### `[<file>...]`

Zero or more files to push. If no files are specified, an empty artifact is created (useful for creating manifests without blobs).

## Options

### Artifact Options

#### `--artifact-type <type>`

**Required.** Artifact type for the manifest (e.g., `application/vnd.myorg.config`).

Example: `--artifact-type application/vnd.acme.config.v1`

#### `--annotation <key=value>`, `-a`

Add annotation to the manifest. Can be specified multiple times.

Example: `--annotation "org.opencontainers.image.title=My Config" --annotation "version=1.0"`

#### `--annotation-file <file>`

Path to a JSON file containing annotations as key-value pairs.

Example annotation file (`annotations.json`):
```json
{
  "org.opencontainers.image.title": "My Application Config",
  "org.opencontainers.image.version": "1.0.0",
  "org.opencontainers.image.created": "2024-01-01T00:00:00Z"
}
```

#### `--export-manifest <file>`

Export the generated manifest to a file after pushing.

Example: `--export-manifest ./manifest.json`

#### `--image-spec <version>`

OCI image spec version to use. Valid values: `v1.0`, `v1.1` (default: `v1.1`).

### Remote Options

#### `--username <username>`, `-u`

Registry username. If not provided, uses stored credentials.

#### `--password <password>`, `-p`

Registry password. If not provided, uses stored credentials.

**Warning:** Providing passwords on the command line may expose them to other users on the system. Use `--password-stdin` or stored credentials instead.

#### `--password-stdin`

Read password from stdin. Useful for piping passwords from credential managers.

Example:
```bash
echo "$PASSWORD" | oras push ghcr.io/myorg/artifact:v1 ./file.txt --username myuser --password-stdin
```

#### `--insecure`

Skip TLS certificate verification (not recommended for production).

#### `--plain-http`

Use HTTP instead of HTTPS.

#### `--ca-file <file>`

Path to custom CA certificate for TLS verification.

#### `--registry-config <file>`

Path to registry configuration file (default: `~/.docker/config.json`).

### Common Options

#### `--concurrency <n>`

Number of concurrent uploads (default: `5`).

Higher values may improve performance for many small files but can overwhelm the registry or network.

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Push a single file

```bash
oras push ghcr.io/myorg/config:v1 ./config.yaml \
  --artifact-type application/vnd.myorg.config.v1
```

### Push multiple files

```bash
oras push ghcr.io/myorg/app:latest \
  ./binary \
  ./config.json \
  ./README.md \
  --artifact-type application/vnd.myorg.app.v1
```

### Push with annotations

```bash
oras push ghcr.io/myorg/artifact:v1 ./file.txt \
  --artifact-type application/vnd.myorg.data \
  --annotation "org.opencontainers.image.title=My Data" \
  --annotation "version=1.0.0" \
  --annotation "author=John Doe"
```

### Push with annotations from file

```bash
oras push ghcr.io/myorg/artifact:v1 ./file.txt \
  --artifact-type application/vnd.myorg.data \
  --annotation-file ./annotations.json
```

### Push and export manifest

```bash
oras push ghcr.io/myorg/artifact:v1 ./file.txt \
  --artifact-type application/vnd.myorg.data \
  --export-manifest ./manifest.json
```

### Push using OCI Image Spec v1.0

```bash
oras push ghcr.io/myorg/artifact:v1 ./file.txt \
  --artifact-type application/vnd.myorg.data \
  --image-spec v1.0
```

### Push with authentication

```bash
oras push ghcr.io/myorg/artifact:v1 ./file.txt \
  --artifact-type application/vnd.myorg.data \
  --username myuser \
  --password-stdin
```

### Push with higher concurrency

```bash
oras push ghcr.io/myorg/artifact:v1 ./*.txt \
  --artifact-type application/vnd.myorg.data \
  --concurrency 10
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (invalid arguments, missing required options, file not found)

## Notes

- Files are uploaded concurrently for better performance
- The artifact type (`--artifact-type`) is required and identifies the purpose of the artifact
- Annotations follow the OCI annotation conventions (see [OCI Image Spec](https://github.com/opencontainers/image-spec/blob/main/annotations.md))
- Use `--export-manifest` to inspect or store the generated manifest for later use
- The manifest digest is printed on successful push

## See Also

- [pull](pull.md) — Pull files from a registry
- [attach](attach.md) — Attach files to an existing artifact
- [tag](tag.md) — Tag a manifest
- [manifest push](manifest-push.md) — Push a pre-built manifest
