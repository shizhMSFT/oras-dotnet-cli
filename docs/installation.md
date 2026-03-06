---
title: Installation
layout: default
nav_order: 2
description: "Platform-specific installation instructions for the ORAS .NET CLI"
---

# Installation Guide

The `oras` .NET CLI can be installed in multiple ways depending on your environment and needs.

## Quick Start

Choose the installation method that works best for you:

- **[.NET Developers](#dotnet-tool)**: `dotnet tool install -g oras`
- **[Binary Download](#binary-download)**: Download pre-built executables for Windows, macOS, or Linux
- **[Build from Source](#build-from-source)**: Clone and build the repository

---

## .NET Tool

If you have the .NET SDK installed, the easiest way to install `oras` is as a global .NET tool:

```bash
dotnet tool install -g oras
```

### Requirements

- .NET 10 SDK or later

### Update

To update to the latest version:

```bash
dotnet tool update -g oras
```

### Uninstall

```bash
dotnet tool uninstall -g oras
```

---

## Binary Download

Download pre-built self-contained binaries from the [GitHub Releases](https://github.com/oras-project/oras-dotnet-cli/releases) page.

### Windows (x64)

```powershell
# Download the latest release
Invoke-WebRequest -Uri "https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/oras-windows-x64.exe" -OutFile "oras.exe"

# Move to a directory in your PATH
Move-Item oras.exe $env:USERPROFILE\bin\oras.exe

# Add to PATH if not already present (PowerShell)
$env:PATH += ";$env:USERPROFILE\bin"
[Environment]::SetEnvironmentVariable("PATH", $env:PATH, [EnvironmentVariableTarget]::User)
```

### Windows (ARM64)

```powershell
Invoke-WebRequest -Uri "https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/oras-windows-arm64.exe" -OutFile "oras.exe"
```

### macOS (Intel x64)

```bash
# Download the latest release
curl -LO https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/oras-darwin-x64

# Make executable and move to PATH
chmod +x oras-darwin-x64
sudo mv oras-darwin-x64 /usr/local/bin/oras
```

### macOS (Apple Silicon ARM64)

```bash
# Download the latest release
curl -LO https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/oras-darwin-arm64

# Make executable and move to PATH
chmod +x oras-darwin-arm64
sudo mv oras-darwin-arm64 /usr/local/bin/oras
```

### Linux (x64)

```bash
# Download the latest release
curl -LO https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/oras-linux-x64

# Make executable and move to PATH
chmod +x oras-linux-x64
sudo mv oras-linux-x64 /usr/local/bin/oras
```

### Linux (ARM64)

```bash
# Download the latest release
curl -LO https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/oras-linux-arm64

# Make executable and move to PATH
chmod +x oras-linux-arm64
sudo mv oras-linux-arm64 /usr/local/bin/oras
```

### Verifying Binary Checksums

Each release includes a `checksums.txt` file. Verify the integrity of your download:

**Windows (PowerShell):**
```powershell
# Download checksums
Invoke-WebRequest -Uri "https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/checksums.txt" -OutFile "checksums.txt"

# Verify checksum
$actualHash = (Get-FileHash -Path oras.exe -Algorithm SHA256).Hash
$expectedHash = (Get-Content checksums.txt | Select-String "oras-windows-x64.exe").Line.Split()[0]
if ($actualHash -eq $expectedHash) { Write-Host "Checksum verified!" } else { Write-Host "Checksum mismatch!" }
```

**macOS/Linux:**
```bash
# Download checksums
curl -LO https://github.com/oras-project/oras-dotnet-cli/releases/latest/download/checksums.txt

# Verify checksum
sha256sum -c checksums.txt --ignore-missing
```

---

## Build from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/oras-project/oras-dotnet-cli.git
cd oras-dotnet-cli

# Build the project
dotnet build -c Release

# Run directly
dotnet run --project src/Oras.Cli -- version

# Or publish as a self-contained executable
dotnet publish src/Oras.Cli -c Release -r linux-x64 --self-contained
```

### Available Runtime Identifiers (RIDs)

- `win-x64` - Windows x64
- `win-arm64` - Windows ARM64
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon
- `linux-x64` - Linux x64
- `linux-arm64` - Linux ARM64

### Install Locally

After building, install as a global .NET tool from the local project:

```bash
dotnet pack src/Oras.Cli -c Release
dotnet tool install -g --add-source ./src/Oras.Cli/bin/Release oras
```

---

## Platform-Specific Notes

### Windows

- **PATH Configuration**: Ensure the directory containing `oras.exe` is in your `PATH` environment variable
- **Execution Policy**: If running into PowerShell execution policy errors, see [shell-completions.md](shell-completions.md#troubleshooting)
- **Windows Defender**: First run may be slower due to antivirus scanning; subsequent runs are fast

### macOS

- **Gatekeeper**: On first run, macOS may block the binary. Right-click and select "Open" to bypass Gatekeeper, or:
  ```bash
  xattr -d com.apple.quarantine /usr/local/bin/oras
  ```
- **Homebrew**: A Homebrew formula may be available in the future for easier installation

### Linux

- **Permissions**: Ensure the binary has execute permissions (`chmod +x oras`)
- **Installation Location**: `/usr/local/bin` is the standard location, but any directory in `$PATH` works
- **SELinux**: If using SELinux, you may need to set the appropriate security context:
  ```bash
  sudo chcon -t bin_t /usr/local/bin/oras
  ```

---

## Verification

After installation, verify `oras` is working:

```bash
# Check version
oras version

# Expected output:
# Version: 1.0.0
# OrasProject.Oras: 0.5.0
# .NET Runtime: 10.0.0
# Platform: linux-x64
```

Test basic functionality:

```bash
# Show help
oras --help

# List available commands
oras --help

# Test a command (requires authentication)
oras version
```

---

## Credential Setup

After installation, you'll need to authenticate with your container registry. See the [Authentication Guide](authentication.md) for details.

Quick example:

```bash
# Login to a registry
oras login ghcr.io

# Verify authentication
oras repo ls ghcr.io/yourusername
```

The CLI uses Docker-compatible credential storage (`~/.docker/config.json`), so existing Docker credentials work automatically.

---

## Next Steps

- [Shell Completions](shell-completions.md) - Enable tab completion for your shell
- [Quick Start Guide](quickstart.md) - Learn basic workflows
- [Command Reference](commands/README.md) - Detailed command documentation
- [Authentication Guide](authentication.md) - Configure registry credentials

---

## Troubleshooting

### Command not found

Ensure the installation directory is in your `PATH`:

```bash
# Check PATH
echo $PATH

# Add to PATH (Bash/Zsh)
export PATH=$PATH:/usr/local/bin
```

### Permission denied

Ensure the binary has execute permissions:

```bash
chmod +x /usr/local/bin/oras
```

### .NET Tool installation fails

Ensure you have the .NET SDK (not just runtime) installed:

```bash
dotnet --version
```

If you only have the runtime, download the SDK from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

### Version mismatch

If `oras version` shows an unexpected version after updating:

```bash
# Clear .NET tool cache
dotnet tool uninstall -g oras
dotnet tool install -g oras

# Or specify the version explicitly
dotnet tool install -g oras --version 1.0.0
```

---

## Supported Platforms

The `oras` CLI officially supports:

| Platform | Architecture | Runtime Identifier |
|----------|--------------|-------------------|
| Windows  | x64          | `win-x64`         |
| Windows  | ARM64        | `win-arm64`       |
| macOS    | Intel (x64)  | `osx-x64`         |
| macOS    | Apple Silicon (ARM64) | `osx-arm64` |
| Linux    | x64          | `linux-x64`       |
| Linux    | ARM64        | `linux-arm64`     |

All binaries are self-contained and require no additional runtime installation.
