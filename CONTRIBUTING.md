# Contributing to oras .NET CLI

Thank you for your interest in contributing to the oras .NET CLI! This document provides guidelines and information for contributors.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10 SDK** (version 10.0.103 or later) - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker** (required for integration tests) - [Install Docker](https://docs.docker.com/get-docker/)
- **Git** for version control

You can verify your .NET SDK version with:
```bash
dotnet --version
```

## Building from Source

Clone the repository and build the solution:

```bash
git clone https://github.com/oras-project/oras-dotnet-cli.git
cd oras-dotnet-cli
dotnet build
```

To build a release configuration:
```bash
dotnet build -c Release
```

## Running Tests

### Unit Tests

Run the unit tests with:
```bash
dotnet test test/oras.Tests/
```

### Integration Tests

Integration tests require Docker to be running. These tests interact with real registries and validate end-to-end scenarios.

```bash
# Ensure Docker is running first
docker ps

# Run all tests including integration tests
dotnet test
```

To run tests with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Project Structure

The project follows a clean architecture with the following structure:

```
oras-dotnet-cli/
├── src/
│   └── Oras.Cli/           # Main CLI application
│       ├── Commands/       # Command implementations (push, pull, login, etc.)
│       ├── Options/        # Command-line options and argument parsers
│       ├── Services/       # Business logic services (registry, push, pull, credentials)
│       ├── Credentials/    # Credential management (Docker config, native helpers)
│       ├── Output/         # Output formatters (JSON, text, progress)
│       └── Tui/            # Terminal UI components (interactive browser, inspector)
├── test/
│   └── oras.Tests/         # Unit and integration tests
├── docs/                   # Documentation
├── .github/                # GitHub workflows and templates
└── artifacts/              # Build output (generated)
```

### Key Directories

- **Commands/**: Each command is implemented as a separate file (e.g., `PushCommand.cs`, `PullCommand.cs`). Commands use System.CommandLine for argument parsing and are registered in `Program.cs`.

- **Options/**: Reusable command-line option classes like `TargetOptions`, `RemoteOptions`, `PlatformOptions`, and `PackerOptions`.

- **Services/**: Core business logic separated from CLI concerns. Includes `IRegistryService`, `IPushService`, `IPullService`, and `ICredentialService` with concrete implementations.

- **Credentials/**: Docker config file parsing and native credential helper integration for secure credential management.

- **Output/**: Output formatting implementations supporting JSON and text formats, plus progress rendering for long-running operations.

- **Tui/**: Interactive terminal UI components including registry browser, manifest inspector, and dashboard using Spectre.Console.

## Code Style

This project follows the .NET coding conventions with specific guidelines defined in `.editorconfig`:

- **File-scoped namespaces**: Use `namespace Oras;` instead of traditional block namespaces
- **Nullable reference types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Indentation**: 4 spaces for C# code
- **Braces**: Always use braces for control statements (enforced as warning)
- **Naming conventions**: 
  - PascalCase for types, methods, and properties
  - Interfaces must start with `I` prefix
  - Use `var` for built-in types and when type is apparent
- **Using statements**: Prefer simple using statements when possible

The project uses EditorConfig to enforce these rules. Most modern IDEs will automatically apply these settings.

### Nullable Reference Types

The project has nullable reference types enabled. Always annotate nullable parameters and return types appropriately:

```csharp
// Good
public string? GetOptionalValue() => null;
public string GetRequiredValue() => "value";

// Bad - will cause compiler warnings
public string GetValue() => null; // Warning: possible null reference return
```

## Pull Request Process

1. **Fork and Branch**: Fork the repository and create a feature branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/bug-description
   ```

2. **Branch Naming**: Use descriptive branch names:
   - `feature/add-resolve-command` for new features
   - `fix/auth-token-expiry` for bug fixes
   - `docs/update-readme` for documentation changes
   - `refactor/service-layer` for refactoring

3. **Make Your Changes**: 
   - Write clean, readable code following the style guide
   - Add tests for new functionality
   - Update documentation as needed

4. **Test Your Changes**: Ensure all tests pass:
   ```bash
   dotnet build
   dotnet test
   ```

5. **Commit Your Changes**: Write clear, descriptive commit messages:
   ```bash
   git commit -m "Add resolve command for digest resolution"
   ```

6. **Push and Create PR**: Push your branch and create a pull request:
   ```bash
   git push origin feature/your-feature-name
   ```

7. **CI Must Pass**: All GitHub Actions workflows must pass before merge. This includes:
   - Build verification on Windows, macOS, and Linux
   - Unit and integration tests
   - Code quality checks

8. **Code Review Required**: All PRs require at least one approving review from a maintainer before merging.

## Running the CLI Locally

To test the CLI during development:

```bash
# Run with dotnet run
dotnet run --project src/Oras.Cli/oras.csproj -- --help

# Run specific commands
dotnet run --project src/Oras.Cli/oras.csproj -- version
dotnet run --project src/Oras.Cli/oras.csproj -- push localhost:5000/test:v1 ./file.txt
```

## Debugging

For Visual Studio or Visual Studio Code:

1. Open the solution file `oras.slnx`
2. Set `Oras.Cli` as the startup project
3. Configure launch settings with command-line arguments in `Properties/launchSettings.json`
4. Press F5 to start debugging

## Getting Help

- **Issues**: Search existing issues or create a new one for bugs or feature requests
- **Discussions**: Use GitHub Discussions for questions and general discussions
- **Documentation**: Check the `docs/` directory for detailed documentation

## Code of Conduct

Be respectful and constructive. We aim to foster an open and welcoming environment for all contributors.

## License

By contributing to this project, you agree that your contributions will be licensed under the Apache License 2.0.
