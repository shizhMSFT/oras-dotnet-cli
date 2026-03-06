---
title: Home
layout: home
nav_order: 1
description: "Cross-platform .NET 10 CLI for managing OCI artifacts in container registries"
permalink: /
---

<div class="hero" markdown="1">

# ORAS .NET CLI
{: .fs-9 }

A cross-platform .NET 10 CLI for managing OCI artifacts in container registries — reimagined with a rich terminal UI.
{: .fs-6 .fw-300 }

[Get Started](#-quick-start){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[Command Reference](commands/){: .btn .fs-5 .mb-4 .mb-md-0 }

</div>

---

[![CI](https://github.com/shizhMSFT/oras-dotnet-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/shizhMSFT/oras-dotnet-cli/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/shizhMSFT/oras-dotnet-cli?include_prereleases&label=release)](https://github.com/shizhMSFT/oras-dotnet-cli/releases)
[![License](https://img.shields.io/github/license/shizhMSFT/oras-dotnet-cli)](https://github.com/shizhMSFT/oras-dotnet-cli/blob/main/LICENSE)

---

## ✨ Features
{: .text-center }

<div class="features-grid" markdown="1">

| 📦 OCI Artifact Management | 🖥️ Interactive TUI | ⚡ Native AOT |
|:---:|:---:|:---:|
| Push, pull, attach, and discover artifacts in any OCI-compliant registry | Beautiful terminal UI powered by Spectre.Console for exploring registries and artifacts | Lightning-fast cold start with self-contained single-file binaries |

| 🌍 Cross-Platform | 🔐 Docker Credentials | 📊 Rich Progress |
|:---:|:---:|:---:|
| Runs on Windows, macOS (Intel & ARM), and Linux (x64 & ARM64) | Seamless authentication using Docker config and credential helpers | Real-time transfer progress with bandwidth and ETA tracking |

</div>

---

## 🚀 Quick Start

### Install

```bash
# Option 1: Download pre-built binary (Linux x64 example)
curl -LO https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-linux-x64.tar.gz
tar -xzf oras-linux-x64.tar.gz && chmod +x oras-linux-x64
sudo mv oras-linux-x64 /usr/local/bin/oras

# Option 2: Build from source
git clone https://github.com/shizhMSFT/oras-dotnet-cli.git
cd oras-dotnet-cli && dotnet build -c Release
```

See the [Installation Guide](installation) for detailed instructions.

### Authenticate

```bash
# Log in to a container registry
oras login ghcr.io -u username -p token

# Log out when done
oras logout ghcr.io
```

### Push & Pull Artifacts

```bash
# Push files as an OCI artifact
oras push ghcr.io/myorg/myartifact:v1.0 \
  ./config.yaml:application/yaml \
  ./data.json:application/json

# Pull an artifact
oras pull ghcr.io/myorg/myartifact:v1.0
```

### Attach & Discover

```bash
# Attach artifacts (signatures, SBOMs)
oras attach ghcr.io/myorg/myartifact:v1.0 \
  --artifact-type signature/example \
  ./signature.sig

# Discover related artifacts
oras discover ghcr.io/myorg/myartifact:v1.0
```

### Backup & Restore

```bash
# Backup an artifact locally
oras backup ghcr.io/myorg/myartifact:v1.0 --output ./backup

# Restore from backup
oras restore ./backup ghcr.io/myorg/myartifact-restored:v1.0
```

### Launch the TUI

```bash
# Open the interactive terminal dashboard
oras

# Tip: Use "Browse Repository Tags" to explore any repo — even on registries
# that don't support catalog listing (like ghcr.io)
```

---

## 📖 Documentation

| Guide | Description |
|:------|:------------|
| [Command Reference](commands/) | Complete reference for all 20 commands and options |
| [Installation Guide](installation) | Platform-specific installation instructions |
| [Migration Guide](migration) | Switching from the Go oras CLI to the .NET CLI |
| [TUI Guide](tui-guide) | Using the interactive terminal UI dashboard |
| [TUI Showcase](tui-showcase) | Visual tour of every TUI screen and feature |
| [Shell Completions](shell-completions) | Tab completion for bash, zsh, PowerShell, fish |

---

## 🏗️ Built With

| Component | Purpose |
|:----------|:--------|
| [System.CommandLine](https://github.com/dotnet/command-line-api) | Modern command-line parsing |
| [Spectre.Console](https://spectreconsole.net/) | Beautiful terminal rendering |
| [OrasProject.Oras](https://github.com/oras-project/oras-dotnet) | Official ORAS .NET library |
| .NET 10 | Self-contained, high-performance runtime |

---

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](https://github.com/shizhMSFT/oras-dotnet-cli/blob/main/CONTRIBUTING.md) for guidelines.

## 🔗 Links

- [ORAS Project](https://oras.land/)
- [Go ORAS CLI](https://github.com/oras-project/oras)
- [OCI Distribution Spec](https://github.com/opencontainers/distribution-spec)
- [Issue Tracker](https://github.com/shizhMSFT/oras-dotnet-cli/issues)
