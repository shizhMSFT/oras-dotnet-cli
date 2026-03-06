# oras login

Log in to a remote registry.

## Synopsis

```bash
oras login <registry> [options]
```

## Description

The `login` command authenticates with a remote registry and stores credentials in the Docker configuration file (`~/.docker/config.json`).

Credentials are stored in a format compatible with Docker and other OCI tools, allowing seamless credential sharing across tools.

## Arguments

### `<registry>`

Registry hostname (e.g., `docker.io`, `ghcr.io`, `localhost:5000`).

The registry can be specified with or without a scheme. If no scheme is provided, HTTPS is assumed unless `--plain-http` is specified.

## Options

### Authentication Options

#### `--username <username>`, `-u`

Registry username. If not provided, you will be prompted interactively.

#### `--password <password>`, `-p`

Registry password. If not provided, you will be prompted interactively.

**Warning:** Providing passwords on the command line may expose them to other users on the system. Use `--password-stdin` or interactive prompts instead.

#### `--password-stdin`

Read password from stdin. This is the recommended way to provide passwords in scripts.

Example:
```bash
echo "$PASSWORD" | oras login ghcr.io --username myuser --password-stdin
```

### Connection Options

#### `--insecure`

Skip TLS certificate verification (not recommended for production).

Use this for registries with self-signed certificates during development.

#### `--plain-http`

Use HTTP instead of HTTPS.

Use this for local registries running without TLS (e.g., `localhost:5000`).

#### `--ca-file <file>`

Path to custom CA certificate for TLS verification.

Use this for registries with certificates signed by a private CA.

### Configuration Options

#### `--registry-config <file>`

Path to registry configuration file (default: `~/.docker/config.json`).

Use this to store credentials in an alternative location.

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Login with interactive prompts

```bash
oras login ghcr.io
# Username: myuser
# Password: ••••••••
```

### Login with username and interactive password

```bash
oras login ghcr.io --username myuser
# Password: ••••••••
```

### Login with username and password from stdin

```bash
echo "$PASSWORD" | oras login ghcr.io --username myuser --password-stdin
```

### Login to local registry with HTTP

```bash
oras login localhost:5000 --plain-http
```

### Login with self-signed certificate

```bash
oras login myregistry.internal --insecure
```

### Login with custom CA certificate

```bash
oras login myregistry.internal --ca-file ./ca-cert.pem
```

### Login with custom config file

```bash
oras login ghcr.io --registry-config ~/.oras/config.json
```

## Exit Codes

- `0` — Success (credentials validated and stored)
- `1` — Authentication failure (invalid credentials, network error)
- `2` — Usage error (invalid arguments)

## Credential Storage

Credentials are stored in the Docker configuration file (`~/.docker/config.json`) in one of two formats:

### 1. Plain text (default)

```json
{
  "auths": {
    "ghcr.io": {
      "auth": "dXNlcm5hbWU6cGFzc3dvcmQ="
    }
  }
}
```

The `auth` field contains base64-encoded `username:password`.

### 2. Credential helpers

If a credential helper is configured, credentials are stored securely using the helper:

```json
{
  "credHelpers": {
    "ghcr.io": "desktop"
  }
}
```

Common credential helpers:
- `docker-credential-desktop` — Docker Desktop
- `docker-credential-pass` — pass (Linux)
- `docker-credential-wincred` — Windows Credential Manager
- `docker-credential-osxkeychain` — macOS Keychain

## Security Considerations

- **Interactive mode**: Passwords are hidden and not echoed to the terminal
- **stdin mode**: Use `--password-stdin` in scripts to avoid exposing passwords in process listings
- **Credential helpers**: Use credential helpers for secure, encrypted storage
- **Plain text storage**: Be aware that credentials in `~/.docker/config.json` are only base64-encoded, not encrypted

## Notes

- Credentials are validated by attempting to authenticate with the registry
- Successful login stores credentials for use by all subsequent `oras` commands
- Credentials are shared with Docker and other OCI tools using the same configuration file
- Use `--plain-http` for local development registries (e.g., `localhost:5000`)
- Use `--insecure` only for development; never use it with production registries

## See Also

- [logout](logout.md) — Log out from a registry
- [push](push.md) — Push artifacts to a registry
- [pull](pull.md) — Pull artifacts from a registry
