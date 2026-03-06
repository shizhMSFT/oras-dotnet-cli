---
title: discover
layout: default
parent: Command Reference
nav_order: 6
---

# oras discover

Discover referrers of a manifest in a registry.

## Synopsis

```bash
oras discover <reference> [options]
```

## Description

The `discover` command lists all artifacts that reference a given manifest. This includes signatures, attestations, SBOMs, and other attachments created with the `attach` command.

Referrers are artifacts with a `subject` field pointing to the queried manifest.

## Arguments

### `<reference>`

Reference to discover referrers for, in the format `[registry/]repository[:tag|@digest]`.

Example: `ghcr.io/myorg/app:v1.0`

## Options

### Filter Options

#### `--artifact-type <type>`

Filter referrers by artifact type.

Example: `--artifact-type application/vnd.cncf.notary.signature`

### Output Options

#### `--format <format>`

Output format: `text` (default) or `json`.

- `text`: Human-readable table format
- `json`: Machine-readable JSON array

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

### Discover all referrers

```bash
oras discover ghcr.io/myorg/app:v1.0
```

Output (text format):
```
Artifact Type                              Digest
application/vnd.cncf.notary.signature      sha256:abc123...
application/vnd.cyclonedx+json             sha256:def456...
application/vnd.in-toto+json               sha256:789ghi...
```

### Discover referrers in JSON format

```bash
oras discover ghcr.io/myorg/app:v1.0 --format json
```

Output:
```json
[
  {
    "digest": "sha256:abc123...",
    "artifactType": "application/vnd.cncf.notary.signature",
    "size": 1234
  },
  {
    "digest": "sha256:def456...",
    "artifactType": "application/vnd.cyclonedx+json",
    "size": 5678
  }
]
```

### Filter by artifact type

```bash
oras discover ghcr.io/myorg/app:v1.0 \
  --artifact-type application/vnd.cncf.notary.signature
```

### Discover by digest

```bash
oras discover ghcr.io/myorg/app@sha256:abc123...
```

### Discover with authentication

```bash
oras discover ghcr.io/myorg/app:v1.0 \
  --username myuser \
  --password-stdin
```

## Exit Codes

- `0` — Success (referrers found or no referrers)
- `1` — Network error, registry error, reference not found
- `2` — Usage error (invalid reference)

## Notes

- Returns empty list if no referrers exist (not an error)
- Use `--artifact-type` to filter by attachment type
- JSON output is useful for scripting and automation
- Referrers are listed with their artifact type and digest
- Use `manifest fetch` to inspect individual referrers by their digest

## Common Artifact Types

| Type | Purpose |
|------|---------|
| `application/vnd.cncf.notary.signature` | Notary v2 signature |
| `application/vnd.cyclonedx+json` | CycloneDX SBOM |
| `application/vnd.spdx+json` | SPDX SBOM |
| `application/vnd.in-toto+json` | in-toto attestation |

## See Also

- [attach](attach.md) — Attach files to an artifact
- [manifest fetch](manifest-fetch.md) — Fetch referrer manifests
- [pull](pull.md) — Pull referrer contents
