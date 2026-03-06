---
title: restore
layout: default
parent: Command Reference
nav_order: 9
---

# oras restore

Restore artifacts from local backup to a registry.

## Synopsis

```bash
oras restore <path> <destination> [options]
```

## Description

The `restore` command pushes artifacts from a local backup (created with `oras backup`) to an OCI-compliant registry. The backup can be in either OCI layout directory format or tar archive format.

With the `--recursive` flag, the command also restores all referrers that were included in the backup.

## Arguments

### `<path>`

Path to the backup. Can be:
- A directory in OCI layout format
- A `.tar.gz` file (tar archive format)

Example: `./backup` or `backup.tar.gz`

### `<destination>`

Destination reference in the format `[registry/]repository[:tag|@digest]`.

Example: `ghcr.io/dest/artifact:v1.0`

## Options

### Restore Options

#### `--recursive`, `-r`

Recursively restore all referrers that are included in the backup.

This restores the entire artifact graph, including signatures, attestations, and other attached artifacts.

### Platform Options

#### `--platform <os/arch>`

Specify platform for multi-platform artifacts (e.g., `linux/amd64`, `linux/arm64`).

When restoring multi-platform backups, this selects a specific platform to restore.

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

### Performance Options

#### `--concurrency <n>`

Number of concurrent upload operations (default: `5`).

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Restore from OCI layout directory

```bash
oras restore ./backup ghcr.io/myorg/artifact:v1.0
```

### Restore from tar archive

```bash
oras restore backup.tar.gz ghcr.io/myorg/artifact:v1.0
```

### Restore with all referrers

```bash
oras restore ./backup ghcr.io/myorg/artifact:v1.0 --recursive
```

### Restore to different registry

```bash
oras restore ./backup docker.io/myorg/artifact:v1.0
```

### Restore with new tag

```bash
oras restore ./backup ghcr.io/myorg/artifact-restored:v1.0
```

### Restore specific platform

```bash
oras restore ./backup ghcr.io/myorg/artifact:v1.0 \
  --platform linux/arm64
```

### Restore with custom concurrency

```bash
oras restore ./backup ghcr.io/myorg/artifact:v1.0 \
  --concurrency 10
```

### Restore to a private registry

```bash
# Login first
oras login ghcr.io -u <username> -p <token>

# Then restore
oras restore ./backup ghcr.io/myorg/artifact:v1.0
```

### Restore from air-gapped environment

```bash
# On a machine with registry access:
oras backup ghcr.io/myorg/artifact:v1.0 --output backup.tar.gz

# Transfer backup.tar.gz to air-gapped environment,
# then restore to the internal registry:
oras restore backup.tar.gz internal-registry:5000/myorg/artifact:v1.0
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (invalid reference, backup path not found)

## Notes

- Restore automatically detects the backup format (OCI layout or tar archive)
- The destination can be different from the original source (useful for disaster recovery)
- Use `--recursive` to restore the complete artifact graph with all referrers
- Credentials are read from the Docker configuration file if stored
- The restored manifest gets a new timestamp but the digest is preserved if the content is identical

## See Also

- [backup](backup.md) — Backup artifacts from a registry
- [copy](copy.md) — Copy artifacts between registries
- [push](push.md) — Push artifacts to a registry
