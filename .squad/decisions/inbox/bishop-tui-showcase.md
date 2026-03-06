# Decision: Terminal Output Blocks Use `text` Fences, Not `ansi`

**Date:** 2026-03-06
**Author:** Bishop (TUI Developer)
**Status:** Proposed

## Context

When showcasing TUI output in the docs site, we needed to choose between ` ```ansi ` and ` ```text ` code fences for terminal output examples.

## Decision

Use ` ```text ` fences for all terminal output blocks in documentation. GitHub Pages / Jekyll with just-the-docs does not render ANSI escape sequences — ` ```ansi ` fences would show raw escape codes instead of colors. Colors and styles are conveyed through descriptive content (Unicode symbols like ✓/⚠/●, structural box-drawing characters) and a Color Reference table.

## Impact

Any future docs pages showing terminal output should follow this convention. If the docs site ever adds a terminal rendering plugin, blocks can be upgraded to ` ```ansi ` later.
