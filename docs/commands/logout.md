---
title: logout
layout: default
parent: Command Reference
nav_order: 2
---

# oras logout

Log out from a remote registry.

## Synopsis

```bash
oras logout <registry> [options]
```

## Description

The `logout` command removes stored credentials for a registry from the Docker configuration file (`~/.docker/config.json`).

After logging out, subsequent operations requiring authentication will fail until you log in again.

## Arguments

### `<registry>`

Registry hostname (e.g., `docker.io`, `ghcr.io`, `localhost:5000`).

Must match the registry name used during login.

## Options

### Configuration Options

#### `--registry-config <file>`

Path to registry configuration file (default: `~/.docker/config.json`).

Use this if you logged in with a custom config file location.

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Logout from a registry

```bash
oras logout ghcr.io
```

### Logout with custom config file

```bash
oras logout ghcr.io --registry-config ~/.oras/config.json
```

### Logout from local registry

```bash
oras logout localhost:5000
```

## Exit Codes

- `0` — Success (credentials removed)
- `1` — Error (file access issue, registry not found in config)
- `2` — Usage error (invalid arguments)

## Notes

- Credentials are removed from the Docker configuration file
- If using credential helpers, the credentials are removed from the helper's storage
- Logging out does not invalidate any active registry sessions or tokens
- After logout, you'll need to log in again to perform authenticated operations

## See Also

- [login](login.md) — Log in to a registry
