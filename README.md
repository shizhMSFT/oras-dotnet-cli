# oras - OCI Registry As Storage CLI

[![Build Status](https://github.com/shizhMSFT/oras-dotnet-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/shizhMSFT/oras-dotnet-cli/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Cross-platform .NET CLI for OCI artifact management with full feature parity to the Go implementation

## Overview

The oras .NET CLI is a production-ready command-line tool for managing OCI artifacts in container registries. Built on [OrasProject.Oras](https://github.com/oras-project/oras-dotnet), it provides native .NET integration with container registries and supports all OCI distribution specification features.

## Features

✨ **Full Go CLI Parity** — 20+ commands covering all artifact operations  
🖥️ **Interactive TUI Mode** — Redesigned v0.2.0 dashboard with FigletText header, context menus, caching, and fully interactive workflows  
🌐 **Cross-Platform** — Works on Windows, macOS, and Linux  
🚀 **Native AOT Ready** — Fast startup and low memory footprint  
🔐 **Secure Credentials** — Docker config and native credential helper support  
📊 **Multiple Output Formats** — JSON and human-readable text output

## Quick Start

### Installation

**Prerequisites**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (version 10.0.103 or later)

```bash
# Clone and build
git clone https://github.com/oras-project/oras-dotnet-cli.git
cd oras-dotnet-cli
dotnet build -c Release

# Run the CLI
dotnet run --project src/Oras.Cli/oras.csproj -- --help
```

For installation instructions including binary downloads, see [docs/installation.md](docs/installation.md).

### Basic Usage

```bash
# Login to a registry
oras login ghcr.io -u username -p password

# Push an artifact
oras push ghcr.io/myuser/myartifact:v1 ./config.json ./data.tar.gz

# Pull an artifact
oras pull ghcr.io/myuser/myartifact:v1

# List repositories
oras repo ls ghcr.io/myuser

# List tags for a repository
oras repo tags ghcr.io/myuser/myartifact

# Discover referrers
oras discover ghcr.io/myuser/myartifact:v1
```

## Command Reference

### Artifact Operations

| Command | Description |
|---------|-------------|
| `push` | Push files to a registry as an artifact |
| `pull` | Pull artifact files from a registry |
| `attach` | Attach files to an existing artifact as a referrer |
| `copy` | Copy an artifact from one location to another |
| `discover` | Discover referrers of a manifest in a registry |
| `resolve` | Resolve a tag to a digest |
| `tag` | Tag a manifest in the registry |

### Manifest Operations

| Command | Description |
|---------|-------------|
| `manifest fetch` | Fetch manifest from a registry |
| `manifest push` | Push manifest to a registry |
| `manifest delete` | Delete a manifest from a registry |
| `manifest fetch-config` | Fetch the config of a manifest |

### Blob Operations

| Command | Description |
|---------|-------------|
| `blob fetch` | Fetch blob content from a registry |
| `blob push` | Push blob content to a registry |
| `blob delete` | Delete a blob from a registry |

### Repository Operations

| Command | Description |
|---------|-------------|
| `repo ls` | List repositories in a registry |
| `repo tags` | List tags for a repository |

### Authentication & Info

| Command | Description |
|---------|-------------|
| `login` | Log in to a container registry |
| `logout` | Log out from a container registry |
| `version` | Display version information |

## Interactive TUI Mode

Launch the interactive terminal UI by running `oras` with no arguments:

```bash
oras
```

The TUI mode provides:
- **Registry Browser**: Navigate repositories and tags interactively
- **Manifest Inspector**: View manifest details, layers, and annotations
- **Dashboard**: Monitor operations and view statistics

*Run `oras` with no arguments to launch the interactive dashboard.*

For details, see [docs/tui-guide.md](docs/tui-guide.md).

## Documentation

- **[Installation Guide](docs/installation.md)** — Binary downloads and installation methods
- **[Command Reference](docs/commands/)** — Detailed documentation for all commands
- **[TUI Guide](docs/tui-guide.md)** — Interactive terminal UI usage
- **[Contributing](CONTRIBUTING.md)** — Contribution guidelines and development setup

## Project Structure

```
oras-dotnet-cli/
├── src/Oras.Cli/          # Main CLI application
│   ├── Commands/          # Command implementations
│   ├── Options/           # CLI option definitions
│   ├── Services/          # Business logic services
│   ├── Credentials/       # Credential management
│   ├── Output/            # Output formatters
│   └── Tui/               # Interactive terminal UI
├── test/oras.Tests/       # Unit and integration tests
└── docs/                  # Documentation
```

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Development setup and prerequisites
- Building and testing guidelines
- Code style and conventions
- Pull request process

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Credits

- Built on [OrasProject.Oras](https://github.com/oras-project/oras-dotnet) — The official ORAS .NET library
- Inspired by [oras-project/oras](https://github.com/oras-project/oras) — The original Go-based ORAS CLI
- Powered by [System.CommandLine](https://github.com/dotnet/command-line-api) and [Spectre.Console](https://spectreconsole.net/)

## Related Projects

- [oras-dotnet](https://github.com/oras-project/oras-dotnet) — The .NET library powering this CLI
- [oras](https://github.com/oras-project/oras) — The original Go-based ORAS CLI
- [ORAS Project](https://oras.land/) — Official ORAS documentation and specifications
