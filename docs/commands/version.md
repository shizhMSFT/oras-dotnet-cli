---
title: version
layout: default
parent: Command Reference
nav_order: 19
---

# oras version

Show version information.

## Synopsis

```bash
oras version [options]
```

## Description

The `version` command displays version information for the `oras` CLI, including:
- CLI version
- OrasProject.Oras library version
- .NET runtime version
- Platform/architecture

## Options

### Common Options

#### `--debug`, `-d`

Enable debug logging to stderr.

#### `--verbose`, `-v`

Enable verbose output.

## Examples

### Show version

```bash
oras version
```

Output:
```
Version: 1.0.0
OrasProject.Oras: 0.5.0
.NET Runtime: 10.0.0
Platform: linux-x64
```

## Exit Codes

- `0` — Success

## Notes

- The version command always succeeds
- No network access is required
- Use this to verify your installation and report issues

## See Also

- [Installation Guide](../installation.md)
