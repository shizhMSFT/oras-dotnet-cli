# oras repo tags

List tags in a repository.

## Synopsis

```bash
oras repo tags <reference> [options]
```

## Description

The `repo tags` command lists all tags in a repository.

## Arguments

### `<reference>`

Repository reference in the format `[registry/]repository`.

Examples:
- `ghcr.io/myorg/app`
- `localhost:5000/myrepo`

Note: Do not include a tag or digest; this command lists all tags.

## Options

### Pagination Options

#### `--last <tag>`

List tags lexically after this tag name (for pagination).

Example: `--last v1.0.0` lists tags after "v1.0.0"

### Output Options

#### `--format <format>`

Output format: `text` (default) or `json`.

- `text`: One tag per line
- `json`: JSON array of tag names

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

### List all tags

```bash
oras repo tags ghcr.io/myorg/app
```

Output (text format):
```
latest
v1.0.0
v1.0.1
v1.1.0
v2.0.0
```

### List in JSON format

```bash
oras repo tags ghcr.io/myorg/app --format json
```

Output:
```json
[
  "latest",
  "v1.0.0",
  "v1.0.1",
  "v1.1.0",
  "v2.0.0"
]
```

### Paginate results

```bash
# Get first page
oras repo tags ghcr.io/myorg/app

# Get next page starting after "v1.1.0"
oras repo tags ghcr.io/myorg/app --last v1.1.0
```

### List from local registry

```bash
oras repo tags localhost:5000/myrepo --plain-http
```

### List with authentication

```bash
oras repo tags ghcr.io/myorg/app --username myuser --password-stdin
```

## Exit Codes

- `0` — Success (tags found or no tags in repository)
- `1` — Network error, registry error, repository not found
- `2` — Usage error (invalid reference)

## Notes

- Returns empty list if repository has no tags (not an error)
- Some registries may require authentication to list tags
- Use `--last` for pagination when dealing with large numbers of tags
- Output order is lexicographic
- Tag names follow OCI distribution spec (alphanumeric, `.`, `_`, `-`)

## See Also

- [repo ls](repo-ls.md) — List repositories in a registry
- [tag](tag.md) — Create new tags
- [resolve](resolve.md) — Resolve tag to digest
