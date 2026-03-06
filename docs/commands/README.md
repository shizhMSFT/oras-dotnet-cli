# Command Reference

Complete reference documentation for all `oras` CLI commands.

## Core Commands

| Command | Description |
|---------|-------------|
| [login](login.md) | Log in to a remote registry |
| [logout](logout.md) | Log out from a remote registry |
| [push](push.md) | Push files to a remote registry |
| [pull](pull.md) | Pull files from a remote registry |
| [version](version.md) | Show version information |

## Registry Operations

| Command | Description |
|---------|-------------|
| [tag](tag.md) | Tag a manifest in a remote registry |
| [resolve](resolve.md) | Resolve a reference to a digest |
| [copy](copy.md) | Copy artifacts between registries |
| [attach](attach.md) | Attach files to an existing artifact |
| [discover](discover.md) | Discover referrers of a manifest |

## Repository Commands

| Command | Description |
|---------|-------------|
| [repo ls](repo-ls.md) | List repositories in a registry |
| [repo tags](repo-tags.md) | List tags in a repository |

## Manifest Commands

| Command | Description |
|---------|-------------|
| [manifest fetch](manifest-fetch.md) | Fetch a manifest from a registry |
| [manifest push](manifest-push.md) | Push a manifest to a registry |
| [manifest delete](manifest-delete.md) | Delete a manifest from a registry |
| [manifest fetch-config](manifest-fetch-config.md) | Fetch the config blob of a manifest |

## Blob Commands

| Command | Description |
|---------|-------------|
| [blob fetch](blob-fetch.md) | Fetch a blob from a registry |
| [blob push](blob-push.md) | Push a blob to a registry |
| [blob delete](blob-delete.md) | Delete a blob from a registry |

## Common Options

Most commands support these common options:

- `--debug`, `-d` — Enable debug logging to stderr
- `--verbose`, `-v` — Enable verbose output
- `--help`, `-h` — Show help for a command

## Remote Options

Commands that interact with registries support these authentication options:

- `--username`, `-u` — Registry username
- `--password`, `-p` — Registry password
- `--password-stdin` — Read password from stdin
- `--insecure` — Skip TLS certificate verification
- `--plain-http` — Use HTTP instead of HTTPS
- `--ca-file` — Path to custom CA certificate
- `--registry-config` — Path to registry configuration file

## Output Formats

Many commands support `--format` option:

- `text` (default) — Human-readable output
- `json` — Machine-readable JSON output

## Platform Selection

Commands that work with multi-platform images support `--platform`:

- `--platform` — Specify platform in format `os/arch` (e.g., `linux/amd64`, `linux/arm64`, `windows/amd64`)

## Exit Codes

All commands follow standard exit code conventions:

| Code | Description |
|------|-------------|
| `0` | Success |
| `1` | General error (network, registry, operation failure) |
| `2` | Usage error (invalid arguments, missing required options) |

## Reference Format

All commands accepting a `<reference>` argument use this format:

```
[registry/]repository[:tag|@digest]
```

Examples:
- `localhost:5000/myrepo:latest`
- `ghcr.io/myorg/myartifact:v1.0`
- `docker.io/library/alpine@sha256:abc123...`

## Getting Help

Each command has detailed help available:

```bash
# Show all commands
oras --help

# Show help for a specific command
oras push --help

# Show help for a subcommand
oras repo ls --help
```

## Quick Examples

**Authentication:**
```bash
oras login ghcr.io
```

**Push artifact:**
```bash
oras push ghcr.io/myorg/myartifact:v1.0 ./file1.txt ./file2.txt --artifact-type application/vnd.myorg.config
```

**Pull artifact:**
```bash
oras pull ghcr.io/myorg/myartifact:v1.0
```

**Copy artifact:**
```bash
oras copy ghcr.io/source/artifact:v1 ghcr.io/dest/artifact:v1
```

**List repositories:**
```bash
oras repo ls ghcr.io
```

For more examples and workflows, see the [Quick Start Guide](../quickstart.md).
