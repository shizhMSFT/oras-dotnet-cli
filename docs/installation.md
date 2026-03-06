---
title: Installation
layout: default
nav_order: 2
description: "Platform-specific installation instructions for the ORAS .NET CLI"
---

# Installation Guide
{: .fs-9 }

Install the ORAS .NET CLI on Windows, macOS, or Linux.
{: .fs-6 .fw-300 }

{: .warning }
> **Early Release** — v0.1.0 is the first release. Some commands have stubbed implementations — see the [Migration Guide](migration) for details.

---

## Quick Start

Choose the installation method that works best for you:

| Method | Best For | Requirements |
|:-------|:---------|:-------------|
| [Binary Download](#binary-download) | Most users — zero dependencies | None |
| [Build from Source](#build-from-source) | Contributors and developers | .NET 10 SDK, Git |

---

## Binary Download

Download self-contained single-file binaries from [GitHub Releases](https://github.com/shizhMSFT/oras-dotnet-cli/releases). No .NET runtime required.

### Windows

<details open markdown="1">
<summary><strong>Windows x64</strong></summary>

```powershell
# Download and extract
Invoke-WebRequest -Uri "https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-win-x64.zip" -OutFile "oras-win-x64.zip"
Expand-Archive oras-win-x64.zip -DestinationPath .

# Move to a directory in your PATH
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\bin" | Out-Null
Move-Item oras-win-x64.exe "$env:USERPROFILE\bin\oras.exe" -Force

# Add to PATH (run once)
$userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($userPath -notlike "*$env:USERPROFILE\bin*") {
    [Environment]::SetEnvironmentVariable("PATH", "$userPath;$env:USERPROFILE\bin", "User")
}
```

</details>

<details markdown="1">
<summary><strong>Windows ARM64</strong></summary>

```powershell
Invoke-WebRequest -Uri "https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-win-arm64.zip" -OutFile "oras-win-arm64.zip"
Expand-Archive oras-win-arm64.zip -DestinationPath .
Move-Item oras-win-arm64.exe "$env:USERPROFILE\bin\oras.exe" -Force
```

</details>

### macOS

<details open markdown="1">
<summary><strong>macOS Apple Silicon (ARM64) — M1/M2/M3/M4</strong></summary>

```bash
# Download and extract
curl -LO https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-osx-arm64.tar.gz
tar -xzf oras-osx-arm64.tar.gz

# Install
chmod +x oras-osx-arm64
sudo mv oras-osx-arm64 /usr/local/bin/oras

# Remove macOS quarantine flag
sudo xattr -d com.apple.quarantine /usr/local/bin/oras
```

</details>

<details markdown="1">
<summary><strong>macOS Intel (x64)</strong></summary>

```bash
curl -LO https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-osx-x64.tar.gz
tar -xzf oras-osx-x64.tar.gz
chmod +x oras-osx-x64
sudo mv oras-osx-x64 /usr/local/bin/oras
sudo xattr -d com.apple.quarantine /usr/local/bin/oras
```

</details>

### Linux

<details open markdown="1">
<summary><strong>Linux x64</strong></summary>

```bash
# Download and extract
curl -LO https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-linux-x64.tar.gz
tar -xzf oras-linux-x64.tar.gz

# Install
chmod +x oras-linux-x64
sudo mv oras-linux-x64 /usr/local/bin/oras
```

</details>

<details markdown="1">
<summary><strong>Linux ARM64</strong></summary>

```bash
curl -LO https://github.com/shizhMSFT/oras-dotnet-cli/releases/download/v0.1.3/oras-linux-arm64.tar.gz
tar -xzf oras-linux-arm64.tar.gz
chmod +x oras-linux-arm64
sudo mv oras-linux-arm64 /usr/local/bin/oras
```

</details>

---

## Build from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git

### Clone and Build

```bash
git clone https://github.com/shizhMSFT/oras-dotnet-cli.git
cd oras-dotnet-cli

# Build
dotnet build -c Release

# Run directly
dotnet run --project src/Oras.Cli -- version

# Or publish a self-contained single-file binary
dotnet publish src/Oras.Cli/oras.csproj -c Release -r linux-x64 \
  --self-contained true -p:PublishSingleFile=true
```

### Available Runtime Identifiers (RIDs)

| RID | Platform |
|:----|:---------|
| `win-x64` | Windows x64 |
| `win-arm64` | Windows ARM64 |
| `osx-x64` | macOS Intel |
| `osx-arm64` | macOS Apple Silicon |
| `linux-x64` | Linux x64 |
| `linux-arm64` | Linux ARM64 |

---

## Verify Installation

After installing, verify `oras` is working:

```bash
oras version
```

Expected output:

```text
Version:          0.1.3
OrasProject.Oras: 0.5.0
.NET Runtime:     10.0.0
Platform:         linux-x64
```

Test the help system:

```bash
oras --help
```

---

## Credential Setup

The CLI uses Docker-compatible credential storage (`~/.docker/config.json`). If you already use Docker or the Go oras CLI, your existing credentials work automatically — no re-login needed.

```bash
# Log in to a container registry
oras login ghcr.io -u <username> -p <token>

# Verify authentication
oras repo ls ghcr.io/<username>
```

See the [Migration Guide](migration) if you're switching from the Go oras CLI.

---

## Platform-Specific Notes

### Windows

- **Windows Defender**: First run may be slower due to antivirus scanning; subsequent runs are fast
- **PATH Configuration**: The install commands above add `%USERPROFILE%\bin` to PATH — restart your terminal after the first install

### macOS

- **Gatekeeper**: The `xattr -d com.apple.quarantine` command in the install steps removes the quarantine flag. Alternatively, right-click → Open in Finder
- **Homebrew**: A Homebrew formula will be available in a future release

### Linux

- **SELinux**: If using SELinux, set the security context: `sudo chcon -t bin_t /usr/local/bin/oras`
- **Installation Location**: `/usr/local/bin` is standard, but any directory in `$PATH` works

---

## Troubleshooting

### Command not found

Ensure the binary's directory is in your `PATH`:

```bash
# Check where oras is
which oras    # macOS/Linux
where oras    # Windows
```

### Permission denied (macOS/Linux)

```bash
chmod +x /usr/local/bin/oras
```

### macOS "cannot be opened" warning

```bash
sudo xattr -d com.apple.quarantine /usr/local/bin/oras
```

---

## Supported Platforms

| Platform | Architecture | Artifact |
|:---------|:-------------|:---------|
| Windows | x64 | `oras-win-x64.zip` |
| Windows | ARM64 | `oras-win-arm64.zip` |
| macOS | Intel (x64) | `oras-osx-x64.tar.gz` |
| macOS | Apple Silicon (ARM64) | `oras-osx-arm64.tar.gz` |
| Linux | x64 | `oras-linux-x64.tar.gz` |
| Linux | ARM64 | `oras-linux-arm64.tar.gz` |

All binaries are self-contained single-file executables — no .NET runtime required.

---

## Next Steps

- [Command Reference](commands/) — All 20 commands documented
- [TUI Showcase](tui-showcase) — See the interactive terminal UI in action
- [Migration Guide](migration) — Switching from the Go oras CLI
- [Shell Completions](shell-completions) — Tab completion for bash, zsh, PowerShell, fish
