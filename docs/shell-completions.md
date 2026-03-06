# Shell Completions

The `oras` CLI provides tab-completion support for Bash, Zsh, PowerShell, and Fish shells using System.CommandLine's built-in completion system.

## Overview

Shell completions enable tab-completion for:
- Command names (e.g., `oras pu<TAB>` → `oras push`)
- Subcommands (e.g., `oras repo <TAB>` → `oras repo ls`, `oras repo tags`)
- Option names (e.g., `oras push --art<TAB>` → `oras push --artifact-type`)
- Option values for restricted options (e.g., `oras manifest fetch --format <TAB>` → `text`, `json`)

## Installation Instructions

### Bash

Add the following to your `~/.bashrc` or `~/.bash_profile`:

```bash
# oras completion
eval "$(oras completion bash)"
```

Alternatively, generate the completion script to a file:

```bash
oras completion bash > ~/.local/share/bash-completion/completions/oras
```

Reload your shell:
```bash
source ~/.bashrc
```

### Zsh

Add the following to your `~/.zshrc`:

```zsh
# oras completion
eval "$(oras completion zsh)"
```

Alternatively, generate the completion script to a file in your `fpath`:

```zsh
oras completion zsh > "${fpath[1]}/_oras"
```

Reload your shell:
```zsh
source ~/.zshrc
```

If you get errors about permissions or completion not loading, ensure the completion directory is in your `fpath`:

```zsh
fpath=(~/.zsh/completion $fpath)
mkdir -p ~/.zsh/completion
oras completion zsh > ~/.zsh/completion/_oras
```

### PowerShell

Add the following to your PowerShell profile (`$PROFILE`):

```powershell
# oras completion
Invoke-Expression (& oras completion powershell | Out-String)
```

Find your profile location:
```powershell
echo $PROFILE
```

If the file doesn't exist, create it:
```powershell
New-Item -Path $PROFILE -ItemType File -Force
```

Reload your profile:
```powershell
. $PROFILE
```

### Fish

Add the following to your `~/.config/fish/config.fish`:

```fish
# oras completion
oras completion fish | source
```

Alternatively, generate the completion script to Fish's completion directory:

```fish
oras completion fish > ~/.config/fish/completions/oras.fish
```

Reload your configuration:
```fish
source ~/.config/fish/config.fish
```

## Usage

Once installed, tab-completion works automatically:

```bash
# Complete command names
oras pu<TAB>          # → oras push

# Complete subcommands
oras repo <TAB>       # → ls, tags

# Complete option names
oras push --<TAB>     # → --artifact-type, --annotation, --concurrency, etc.

# Complete option values
oras manifest fetch --format <TAB>   # → text, json
```

## Verifying Installation

To verify completions are working:

1. Type `oras <TAB><TAB>` — you should see a list of available commands
2. Type `oras push --<TAB><TAB>` — you should see available options
3. If nothing appears, ensure you've reloaded your shell configuration

## Troubleshooting

### Bash: Completions not working

Ensure `bash-completion` package is installed:

```bash
# Ubuntu/Debian
sudo apt-get install bash-completion

# macOS (via Homebrew)
brew install bash-completion@2
```

### Zsh: Command not found: compdef

Enable completion system in `~/.zshrc` before the completion script:

```zsh
autoload -Uz compinit
compinit
```

### PowerShell: Security error executing script

Set execution policy to allow local scripts:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

### General: Completions outdated after CLI update

Regenerate and reload the completion script:

```bash
# Bash
oras completion bash > ~/.local/share/bash-completion/completions/oras
source ~/.bashrc

# Zsh
oras completion zsh > ~/.zsh/completion/_oras
source ~/.zshrc

# PowerShell
. $PROFILE

# Fish
oras completion fish > ~/.config/fish/completions/oras.fish
source ~/.config/fish/config.fish
```

## Custom Completions

The CLI uses System.CommandLine's intelligent completion engine, which automatically provides:
- Command and subcommand names
- Option names and aliases
- Enum-based option values (e.g., `--format text|json`)

For file path arguments, your shell's native file completion will work automatically.

## Technical Details

The completion system is provided by System.CommandLine 2.x and requires no manual maintenance. Completions are generated dynamically from the command tree structure defined in the CLI application.

The `oras completion` command invokes the built-in directive handler that outputs shell-specific completion scripts. These scripts integrate with each shell's completion system.
