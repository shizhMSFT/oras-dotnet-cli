---
title: backup
layout: default
parent: Command Reference
nav_order: 8
---

# oras backup

Backup artifacts from a registry to local storage.

## Synopsis

```bash
oras backup <reference> [options]
```

## Description

The `backup` command saves an OCI artifact and its contents to a local OCI layout directory or tar archive. This is useful for disaster recovery, creating offline copies, and air-gapped environments.

With the `--recursive` flag, the command also backs up all referrers (attached artifacts like signatures and SBOMs), creating a complete copy of the artifact graph.

## Arguments

### `<reference>`

Reference to backup in the format `[registry/]repository[:tag|@digest]`.

Example: `ghcr.io/myorg/artifact:v1.0`

## Options

### Backup Options

#### `--output <path>`, `-o`

Output path for the backup. Can be:
- A directory path for OCI layout format (default)
- A `.tar.gz` file path for tar archive format

Default: `./oras-backup`

#### `--recursive`, `-r`

Recursively backup all referrers (artifacts attached to the source artifact).

This backs up the entire artifact graph, including signatures, attestations, and other attached artifacts.

### Platform Options

#### `--platform <os/arch>`

Specify platform for multi-platform artifacts (e.g., `linux/amd64`, `linux/arm64`).

When backing up multi-platform images, this selects a specific platform to backup.

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

Number of concurrent download operations (default: `5`).

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Backup to OCI layout directory

```bash
oras backup ghcr.io/myorg/artifact:v1.0 --output ./backup
```

### Backup to tar archive

```bash
oras backup ghcr.io/myorg/artifact:v1.0 --output backup.tar.gz
```

### Backup with all referrers

```bash
oras backup ghcr.io/myorg/artifact:v1.0 --output ./backup --recursive
```

### Backup specific platform

```bash
oras backup ghcr.io/myorg/multiplatform:latest --output ./backup \
  --platform linux/arm64
```

### Backup by digest

```bash
oras backup ghcr.io/myorg/artifact@sha256:abc123... --output ./backup
```

### Backup with custom concurrency

```bash
oras backup ghcr.io/myorg/artifact:v1.0 --output ./backup \
  --concurrency 10
```

### Backup from a private registry

```bash
# Login first
oras login ghcr.io -u <username> -p <token>

# Then backup
oras backup ghcr.io/myorg/artifact:v1.0 --output ./backup
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (invalid reference, path issues)

## Notes

- Backup supports both OCI layout directories and tar archives
- OCI layout format preserves the exact registry structure; tar archives compress for easier transport
- Backup includes only the referenced artifact and blobs; manifests are stored in the OCI layout
- Use `--recursive` to create a complete backup of an artifact graph with referrers
- Credentials are read from the Docker configuration file if stored
- Output directory is created if it doesn't exist

## See Also

- [restore](restore.md) — Restore artifacts from a local backup to a registry
- [copy](copy.md) — Copy artifacts between registries
- [pull](pull.md) — Pull artifacts from a registry
