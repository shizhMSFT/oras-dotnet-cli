# ORAS .NET CLI

> A cross-platform .NET 10 CLI for managing OCI artifacts in container registries

[![Build Status](https://github.com/oras-project/oras-dotnet-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/oras-project/oras-dotnet-cli/actions/workflows/ci.yml)
[![Release](https://github.com/oras-project/oras-dotnet-cli/actions/workflows/release.yml/badge.svg)](https://github.com/oras-project/oras-dotnet-cli/releases)
[![License](https://img.shields.io/github/license/oras-project/oras-dotnet-cli)](LICENSE)

ORAS (OCI Registry As Storage) is a .NET reimagining of the Go ORAS CLI, providing powerful command-line tools for pushing, pulling, and managing OCI artifacts in container registries. Built on the `OrasProject.Oras` library with native AOT compilation for blazing-fast startup times.

## ✨ Features

- **OCI Artifact Management**: Push, pull, attach, and discover artifacts in any OCI-compliant registry
- **Interactive TUI**: Beautiful terminal UI powered by Spectre.Console for exploring registries and artifacts
- **Native AOT**: Lightning-fast cold start with self-contained single-file binaries
- **Cross-Platform**: Runs on Windows, macOS (Intel & ARM), and Linux (x64 & ARM64)
- **Docker Credential Integration**: Seamless authentication using Docker config and credential helpers
- **Rich Progress Indicators**: Real-time transfer progress with bandwidth and ETA tracking

## 🚀 Getting Started

### Installation

**Download Pre-built Binaries** (Recommended):
```bash
# Download the latest release for your platform
# Available: oras-win-x64.exe, oras-linux-x64, oras-osx-arm64, etc.
```

**Install as .NET Tool**:
```bash
dotnet tool install -g oras
```

**Build from Source**:
```bash
git clone https://github.com/oras-project/oras-dotnet-cli.git
cd oras-dotnet-cli
dotnet build -c Release
```

For detailed installation instructions, see [Installation Guide](installation.md).

### Quick Start

```bash
# Login to a registry
oras login ghcr.io -u username -p token

# Push an artifact
oras push ghcr.io/myorg/myartifact:v1.0 \
  ./config.yaml:application/yaml \
  ./data.json:application/json

# Pull an artifact
oras pull ghcr.io/myorg/myartifact:v1.0

# Attach artifacts (signatures, SBOMs)
oras attach ghcr.io/myorg/myartifact:v1.0 \
  --artifact-type signature/example \
  ./signature.sig

# Discover related artifacts
oras discover ghcr.io/myorg/myartifact:v1.0

# Launch interactive TUI
oras tui
```

## 📖 Documentation

- **[Command Reference](commands/)** - Complete reference for all commands and options
- **[Installation Guide](installation.md)** - Platform-specific installation instructions
- **[TUI Guide](tui-guide.md)** - Using the interactive terminal UI
- **[Shell Completions](shell-completions.md)** - Tab completion for bash, zsh, PowerShell, fish

## 🏗️ Architecture

ORAS .NET CLI is built on:
- **[System.CommandLine](https://github.com/dotnet/command-line-api)** - Modern command-line parsing
- **[Spectre.Console](https://spectreconsole.net/)** - Beautiful terminal rendering
- **[OrasProject.Oras](https://github.com/oras-project/oras-dotnet)** - Official ORAS .NET library
- **.NET 10 Native AOT** - Self-contained, high-performance binaries

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## 📄 License

Licensed under the Apache License, Version 2.0. See [LICENSE](../LICENSE) for details.

## 🔗 Links

- **[Project Homepage](https://oras.land/)**
- **[ORAS Specification](https://github.com/oras-project/artifacts-spec)**
- **[Go ORAS CLI](https://github.com/oras-project/oras)**
- **[Issue Tracker](https://github.com/oras-project/oras-dotnet-cli/issues)**
