# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```shell
dotnet restore                    # Restore packages (use --locked-mode in CI)
dotnet build                      # Build all projects
dotnet pack src/SourceExplorerMcp # Pack as dotnet tool
dotnet run --project src/SourceExplorerMcp -- [args]  # Run the MCP server locally
```

No tests exist yet. Lock files are enabled; update with `dotnet restore --force-evaluate`.

## Project Overview

An MCP server distributed as a .NET 10 dotnet tool that lets AI agents explore and decompile .NET assemblies from a
project's dependency graph. Uses stdio transport.

## Architecture

Two projects in `src/`: the MCP server entry point (`SourceExplorerMcp`) and a core library (`SourceExplorerMcp.Core`)
with no MCP dependency. All services are singletons wired in `Program.cs`.

### Data Flow

The system works by chaining: parse `project.assets.json` to map DLL filenames to NuGet packages → scan `bin/`
directories for matching `.dll` files → index types via `System.Reflection.Metadata` → decompile on demand via
`ICSharpCode.Decompiler`. Assembly and type data are cached in `IMemoryCache` keyed by normalised base path, so repeated
tool calls against the same project are fast.

### MCP Tool Pattern

Each tool in `Tools/` follows the same structure: a class with `[McpServerToolType]`, a single method with
`[McpServerTool]`, and companion input/output record types where `[Description]` attributes drive MCP schema generation.
The tool descriptions are intentionally verbose — they serve as agent-facing documentation.

## Code Conventions

- .NET 10, C# 14, nullable enabled, warnings as errors
- Central package management via `src/Directory.Packages.props`
- Versioning handled by GitVersion
- EditorConfig: UTF-8, LF line endings, 4-space indent, 120 char max line length
