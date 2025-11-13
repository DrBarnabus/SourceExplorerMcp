# SourceExplorerMcp

#### MCP tools for exploring source code of .NET assemblies via decompilation

---

## What is SourceExplorerMcp

**SourceExplorerMcp** is a dotnet tool, or more specifically, a [ModelContextProtocol][mcp] server built to allow for
the exploration of source code for .NET assemblies via decompilation.

The server exposes a number of MCP tools to allow agents such as Claude Code to explore and decompile .NET assemblies/types.

### Available tools

- *list-all-assemblies* - Lists all restored assemblies.
- *search-types* - Search for types in restored assemblies based on a search string.
- *decompile-type* - Decompile a specific type from a restored assembly, and return the C# source.

<!-- Links -->
[mcp]: https://modelcontextprotocol.io/docs/getting-started/intro
