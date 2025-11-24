# SourceExplorerMcp

#### MCP tools for exploring source code of .NET assemblies via decompilation

[![GitHub Release][gh-release-badge]][gh-release]
[![NuGet Downloads][nuget-downloads-badge]][nuget-downloads]
[![Build Status][gh-actions-badge]][gh-actions]

---

## What is SourceExplorerMcp

**SourceExplorerMcp** is a dotnet tool, or more specifically, a [ModelContextProtocol][mcp] server built to allow for
the exploration of source code for .NET assemblies via decompilation.

The server exposes a number of MCP tools to allow agents such as Claude Code to explore and decompile .NET assemblies/types.

### Available tools

- *list-all-assemblies* - Lists all restored assemblies.
- *search-types* - Search for types in restored assemblies based on a search string.
- *decompile-type* - Decompile a specific type from a restored assembly and return the C# source.

## Installation

This MCP is distributed as a [dotnet tool][dotnet-tool], the preferred way to install this is with the new `dnx` option
included with .NET 10

### Prerequisites

- [.NET 10 SDK][dotnet-10]

### Configure the MCP Server in your client

**Claude Code**:
```shell
claude mcp add source-explorer -- dotnet dnx SourceExplorerMcp --prerelease --yes
```

> Note: You can also add `--scope local`, `--scope user` or `--scope project` to the above command.
> It should default to `local` if not specified.

**Standard Config** (works in most tools):
```json
{
    "mcpServers": {
        "source-explorer": {
            "type": "stdio",
            "command": "dotnet",
            "args": [
                "dnx",
                "SourceExplorerMcp",
                "--prerelease",
                "--yes"
            ]
        }
    }
}
```

<!-- Badges -->
[gh-release-badge]: https://img.shields.io/github/v/release/DrBarnabus/SourceExplorerMcp?color=g&style=for-the-badge
[gh-release]: https://github.com/DrBarnabus/SourceExplorerMcp/releases/latest
[nuget-downloads-badge]: https://img.shields.io/nuget/dt/SourceExplorerMcp?color=g&logo=nuget&style=for-the-badge
[nuget-downloads]: https://www.nuget.org/packages/SourceExplorerMcp
[gh-actions-badge]: https://img.shields.io/github/actions/workflow/status/DrBarnabus/SourceExplorerMcp/ci.yml?logo=github&branch=main&style=for-the-badge
[gh-actions]: https://github.com/DrBarnabus/SourceExplorerMcp/actions/workflows/ci.yml

<!-- Links -->
[mcp]: https://modelcontextprotocol.io/docs/getting-started/intro
[dotnet-tool]: https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools
[dotnet-10]: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
