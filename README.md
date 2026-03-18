# Shell (C#)

A custom shell implementation in C# built as a [CodeCrafters](https://codecrafters.io/) challenge. Supports builtins, piping, I/O redirection, tab completion, and command history.

## Features

### Builtin Commands
- `echo` - Print text to stdout
- `pwd` - Print current working directory
- `cd` - Change directory (supports `~` for home)
- `exit` / `quit` - Exit the shell
- `type` - Show if a command is a builtin or external program
- `history` - View and manage command history
  - `history N` - Show last N commands
  - `history -w FILE` - Write history to file
  - `history -a FILE` - Append new entries to file
  - `history -r FILE` - Read history from file

### Piping
- Chain commands with `|`
- Supports multiple pipes (`cmd1 | cmd2 | cmd3`)
- Builtins work in pipelines

### I/O Redirection
- `>` / `1>` - Redirect stdout (overwrite)
- `>>` / `1>>` - Append stdout
- `2>` - Redirect stderr (overwrite)
- `2>>` - Append stderr

### Quoting & Escaping
- Single quotes for literal strings
- Double quotes with escape sequence support
- Backslash escaping for special characters

### Tab Completion
- Completes builtin commands and PATH executables
- Longest common prefix matching for multiple suggestions
- Double-tab to list all matches

### Command History
- In-memory history during session
- Persistent history via `HISTFILE` environment variable
- Auto-loads on startup, saves on exit

## Tech Stack

- C# / .NET 9.0
- [ReadLine.Ext](https://www.nuget.org/packages/ReadLine.Ext/) (v0.0.9) - Interactive readline with history and autocomplete

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Installation & Setup

### Build and run

```bash
dotnet build --configuration Release
dotnet run
```

Or use the provided script:

```bash
./your_program.sh
```

### Environment Variables

- `HISTFILE` - Path to a file for persistent command history (optional)

## Project Structure

```
shell-csharp/
├── codecrafters-shell.csproj
├── codecrafters-shell.sln
├── your_program.sh
└── src/
    ├── main.cs                  # Shell loop and builtin handlers
    ├── TokenizationHandler.cs   # Input parsing with quote/escape handling
    ├── PipelineHandler.cs       # Multi-stage pipeline execution
    ├── AutoCompleteHandler.cs   # Tab completion for builtins and PATH
    └── HistoryHandler.cs        # Command history and persistence
```
