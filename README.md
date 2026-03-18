# SourceExplorerMcp

#### MCP tools for exploring source code of .NET assemblies via decompilation

[![GitHub Release][gh-release-badge]][gh-release]
[![NuGet Downloads][nuget-downloads-badge]][nuget-downloads]
[![Build Status][gh-actions-badge]][gh-actions]
[![License: MIT][licence-badge]][licence]

---

## What is SourceExplorerMcp

**SourceExplorerMcp** is a [Model Context Protocol][mcp] (MCP) server distributed as a [dotnet tool][dotnet-tool]. It
lets AI agents explore and decompile .NET types from a project's dependency graph — NuGet packages and framework
assemblies — without needing the original source code.

Point it at any .NET project directory, and the server will resolve the project's dependencies, scan for assemblies,
and expose tools that let an agent search for types and decompile them into readable C# source.

### How it works

1. Parses `project.assets.json` to map DLL filenames to NuGet packages.
2. Scans `bin/` directories for matching `.dll` files.
3. Indexes types via `System.Reflection.Metadata`.
4. Decompiles on demand via `ICSharpCode.Decompiler`.

Assembly and type data are cached in memory, so repeated tool calls against the same project are fast.

### Available tools

| Tool               | Description                                                                                                                                                                                                         |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **search-types**   | Search for .NET types across all assemblies in a project's dependency graph using wildcard patterns (e.g. `*HttpClient*`, `Microsoft.Extensions.*Options`). Returns type summaries including fully-qualified names. |
| **decompile-type** | Decompile a .NET type into C# source code. Supports `full` mode (complete source with method bodies) and `signatures` mode (API surface only). Large outputs are automatically offloaded to a temporary file.       |

## Installation

### Prerequisites

- [.NET 10 SDK][dotnet-10]

### Configure the MCP server in your client

**Claude Code**:
```shell
claude mcp add source-explorer -- dotnet dnx SourceExplorerMcp --yes
```

> You can also add `--scope local`, `--scope user` or `--scope project` to the above command.
> It defaults to `local` if not specified.

**Standard Config** (works in most MCP clients):
```json
{
    "mcpServers": {
        "source-explorer": {
            "type": "stdio",
            "command": "dotnet",
            "args": [
                "dnx",
                "SourceExplorerMcp",
                "--yes"
            ]
        }
    }
}
```

### Configuration

| Environment Variable               | Default | Description                                                                                               |
|------------------------------------|---------|-----------------------------------------------------------------------------------------------------------|
| `SOURCE_EXPLORER_MAX_INLINE_CHARS` | `10000` | Character threshold before decompiled source is offloaded to a temporary file instead of returned inline. |

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a full list of changes between versions.

## Licence

This project is licenced under the [MIT Licence](LICENSE).

<!-- Badges -->
[gh-release-badge]: https://img.shields.io/github/v/release/DrBarnabus/SourceExplorerMcp?color=g&style=for-the-badge
[gh-release]: https://github.com/DrBarnabus/SourceExplorerMcp/releases/latest
[nuget-downloads-badge]: https://img.shields.io/nuget/dt/SourceExplorerMcp?color=g&logo=nuget&style=for-the-badge
[nuget-downloads]: https://www.nuget.org/packages/SourceExplorerMcp
[gh-actions-badge]: https://img.shields.io/github/actions/workflow/status/DrBarnabus/SourceExplorerMcp/ci.yml?logo=github&branch=main&style=for-the-badge
[gh-actions]: https://github.com/DrBarnabus/SourceExplorerMcp/actions/workflows/ci.yml
[licence-badge]: https://img.shields.io/badge/licence-MIT-g?style=for-the-badge
[licence]: https://github.com/DrBarnabus/SourceExplorerMcp/blob/main/LICENSE

<!-- Links -->
[mcp]: https://modelcontextprotocol.io/docs/getting-started/intro
[dotnet-tool]: https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools
[dotnet-10]: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
