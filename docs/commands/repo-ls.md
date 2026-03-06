# oras repo ls

List repositories in a registry.

## Synopsis

```bash
oras repo ls <registry> [options]
```

## Description

The `repo ls` command lists all repositories (namespaces) in a registry that the authenticated user has access to.

This is useful for discovering what content exists in a registry.

## Arguments

### `<registry>`

Registry hostname (e.g., `docker.io`, `ghcr.io`, `localhost:5000`).

## Options

### Pagination Options

#### `--last <repository>`

List repositories lexically after this repository name (for pagination).

Example: `--last myorg/app` lists repositories after "myorg/app"

### Output Options

#### `--format <format>`

Output format: `text` (default) or `json`.

- `text`: One repository per line
- `json`: JSON array of repository names

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

### List all repositories

```bash
oras repo ls ghcr.io
```

Output (text format):
```
myorg/app
myorg/config
myorg/library
otherorg/project
```

### List in JSON format

```bash
oras repo ls ghcr.io --format json
```

Output:
```json
[
  "myorg/app",
  "myorg/config",
  "myorg/library",
  "otherorg/project"
]
```

### Paginate results

```bash
# Get first page
oras repo ls ghcr.io

# Get next page starting after "myorg/library"
oras repo ls ghcr.io --last myorg/library
```

### List from local registry

```bash
oras repo ls localhost:5000 --plain-http
```

### List with authentication

```bash
oras repo ls ghcr.io --username myuser --password-stdin
```

## Exit Codes

- `0` — Success (repositories found or empty registry)
- `1` — Network error, registry error, authentication failure
- `2` — Usage error (invalid registry)

## Notes

- Returns empty list if no repositories exist or user has no access (not an error)
- Some registries may require authentication to list repositories
- Use `--last` for pagination when dealing with large numbers of repositories
- The list may be filtered by the registry based on user permissions
- Output order is lexicographic

## Registry Compatibility

Not all registries support repository listing:
- ✅ Docker Hub
- ✅ GitHub Container Registry (ghcr.io)
- ✅ Azure Container Registry
- ✅ Harbor
- ⚠️ Some private registries may disable this API

If a registry doesn't support listing, an error will be returned.

## See Also

- [repo tags](repo-tags.md) — List tags in a repository
- [discover](discover.md) — Discover referrers
