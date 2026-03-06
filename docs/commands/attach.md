# oras attach

Attach files to an existing artifact in a registry.

## Synopsis

```bash
oras attach <reference> [<file>...] --artifact-type <type> [options]
```

## Description

The `attach` command creates a new artifact that references an existing artifact (called the "subject"). This is used to attach signatures, attestations, SBOMs, or other metadata to an existing artifact.

The attached artifact is stored as a separate manifest with a `subject` field pointing to the original artifact. It appears as a referrer when using the `discover` command.

## Arguments

### `<reference>`

Subject reference (the artifact to attach to) in the format `[registry/]repository[:tag|@digest]`.

Example: `ghcr.io/myorg/app:v1.0`

### `[<file>...]`

Zero or more files to attach. Can be empty if you only want to create a reference with annotations.

## Options

### Required Options

#### `--artifact-type <type>`

**Required.** Artifact type for the attached artifact (e.g., `application/vnd.example.signature.v1`).

This identifies the purpose of the attachment (signature, SBOM, etc.).

### Artifact Options

#### `--annotation <key=value>`, `-a`

Add annotation to the manifest. Can be specified multiple times.

Example: `--annotation "signature.algorithm=RSA-PSS" --annotation "signature.keyid=key1"`

#### `--annotation-file <file>`

Path to a JSON file containing annotations.

#### `--export-manifest <file>`

Export the generated manifest to a file.

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

Number of concurrent uploads (default: `5`).

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Attach a signature

```bash
oras attach ghcr.io/myorg/app:v1.0 ./signature.sig \
  --artifact-type application/vnd.example.signature.v1 \
  --annotation "signature.algorithm=RSA-PSS"
```

### Attach an SBOM

```bash
oras attach ghcr.io/myorg/app:v1.0 ./sbom.json \
  --artifact-type application/vnd.cyclonedx+json \
  --annotation "org.opencontainers.image.title=SBOM"
```

### Attach multiple files

```bash
oras attach ghcr.io/myorg/app:v1.0 \
  ./signature.sig \
  ./provenance.json \
  --artifact-type application/vnd.example.security-bundle.v1
```

### Attach with annotations file

```bash
oras attach ghcr.io/myorg/app:v1.0 ./attestation.json \
  --artifact-type application/vnd.example.attestation.v1 \
  --annotation-file ./metadata.json
```

### Attach to digest (immutable reference)

```bash
oras attach ghcr.io/myorg/app@sha256:abc123... ./signature.sig \
  --artifact-type application/vnd.example.signature.v1
```

### Attach and export manifest

```bash
oras attach ghcr.io/myorg/app:v1.0 ./sbom.json \
  --artifact-type application/vnd.cyclonedx+json \
  --export-manifest ./attached-manifest.json
```

## Exit Codes

- `0` — Success
- `1` — Network error, registry error, or operation failure
- `2` — Usage error (missing `--artifact-type`, file not found, invalid reference)

## Notes

- The `--artifact-type` option is required
- Attachments are stored as separate manifests with a `subject` field
- Use `discover` to list all attachments for an artifact
- Attachments can have their own attachments (nested referrers)
- Consider attaching to digest rather than tag for immutable references
- The attachment digest is printed on successful attach

## Attachment Types

Common artifact types for attachments:

| Type | Purpose |
|------|---------|
| `application/vnd.oci.image.config.v1+json` | Configuration |
| `application/vnd.cncf.notary.signature` | Notary v2 signature |
| `application/vnd.cyclonedx+json` | CycloneDX SBOM |
| `application/vnd.spdx+json` | SPDX SBOM |
| `application/vnd.in-toto+json` | in-toto attestation |
| `application/vnd.example.signature.v1` | Custom signature format |

## See Also

- [discover](discover.md) — Discover referrers (attachments)
- [push](push.md) — Push artifacts to a registry
- [manifest fetch](manifest-fetch.md) — Fetch manifests
