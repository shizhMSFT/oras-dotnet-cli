# oras tag

Tag a manifest in a remote registry.

## Synopsis

```bash
oras tag <source> <tag> [<tag>...] [options]
```

## Description

The `tag` command creates one or more new tags pointing to an existing manifest in a registry. The source can be specified by tag or digest.

This operation does not copy the manifest or blobs; it creates new references to the same content.

## Arguments

### `<source>`

Source reference in the format `[registry/]repository[:tag|@digest]`.

Examples:
- `ghcr.io/myorg/artifact:v1.0`
- `ghcr.io/myorg/artifact@sha256:abc123...`

### `<tag>...`

One or more destination tags to create.

Tags must be valid according to OCI distribution spec (alphanumeric, `.`, `_`, `-`).

## Options

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

### Create a single tag

```bash
oras tag ghcr.io/myorg/artifact:v1.0 v1.0.1
```

### Create multiple tags

```bash
oras tag ghcr.io/myorg/artifact:v1.0 v1.0.1 v1 latest
```

### Tag by digest

```bash
oras tag ghcr.io/myorg/artifact@sha256:abc123... stable production
```

### Tag with authentication

```bash
oras tag ghcr.io/myorg/artifact:v1.0 v1.0.1 \
  --username myuser \
  --password-stdin
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (invalid reference, invalid tag name)

## Notes

- Tagging is a metadata operation; no blobs are copied
- All tags must be in the same repository as the source
- Tags are created atomically
- If any tag creation fails, the operation stops and returns an error

## See Also

- [push](push.md) — Push artifacts to a registry
- [resolve](resolve.md) — Resolve a tag to a digest
- [copy](copy.md) — Copy artifacts between repositories
