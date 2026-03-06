# oras - OCI Registry As Storage CLI

A cross-platform .NET 10 CLI for managing OCI artifacts in container registries. Built on the [oras-dotnet](https://github.com/oras-project/oras-dotnet) library, this tool brings the power of OCI Registry As Storage to .NET developers.

## Features

- **Push and Pull OCI Artifacts**: Manage any file type in OCI-compliant registries
- **Cross-Platform**: Works on Windows, macOS, and Linux
- **Modern .NET**: Built with .NET 10 and modern C# features
- **.NET Native**: Leverages the OrasProject.Oras library for native .NET integration

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.103 or later)

## Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/oras-dotnet-cli.git
cd oras-dotnet-cli

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the CLI
dotnet run --project src/oras/oras.csproj -- --help
```

## Basic Usage

```bash
# Display help
oras --help

# Push an artifact
oras push <registry>/<repository>:<tag> <file>

# Pull an artifact
oras pull <registry>/<repository>:<tag>

# List artifacts
oras repo ls <registry>/<repository>
```

## Project Structure

```
oras-dotnet-cli/
├── src/
│   └── oras/              # Main CLI application
├── test/
│   └── oras.Tests/        # Unit and integration tests
├── .github/
│   ├── ISSUE_TEMPLATE/    # GitHub issue templates
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── workflows/         # CI/CD workflows
├── global.json            # .NET SDK version pinning
├── Directory.Build.props  # Shared MSBuild properties
└── oras.sln              # Solution file
```

## Contributing

Contributions are welcome! Please see our [contributing guidelines](CONTRIBUTING.md) for details.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Related Projects

- [oras-dotnet](https://github.com/oras-project/oras-dotnet) - The .NET library powering this CLI
- [oras](https://github.com/oras-project/oras) - The original Go-based ORAS CLI
ORAS CLI based on ORAS .NET SDK
