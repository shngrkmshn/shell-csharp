# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a CodeCrafters "Build Your Own Shell" challenge implementation in C#. The project implements a basic Unix-style shell with command parsing, builtin commands, external program execution, and I/O redirection.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the shell
dotnet run

# Build and run in one step
dotnet run --project codecrafters-shell.csproj
```

## Architecture

The shell consists of three main components:

- **main.cs** - Core shell loop and command execution. Handles:
  - REPL loop with ReadLine library for input
  - Builtin commands: `echo`, `exit`, `pwd`, `cd`, `type`
  - External program execution via `Process.Start`
  - I/O redirection (`>`, `>>`, `2>`, `2>>`, `1>`, `1>>`)
  - PATH-based executable lookup

- **TokenizationHandler.cs** - Input parsing with shell quoting rules:
  - Single quotes (literal, no escape processing)
  - Double quotes (with backslash escapes for `"` and `\`)
  - Backslash escaping outside quotes

- **AutoCompleteHandler.cs** - Tab completion for builtin commands using the ReadLine library

## Key Implementation Notes

- Uses `File.GetUnixFileMode()` to check executable permissions (Linux-specific)
- External programs run with `UseShellExecute = false` and working directory set to the executable's directory
- Redirection operators are parsed from tokenized input and removed before command execution