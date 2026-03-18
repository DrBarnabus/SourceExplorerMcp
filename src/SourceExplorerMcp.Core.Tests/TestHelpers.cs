namespace SourceExplorerMcp.Core.Tests;

internal static class TestHelpers
{
    public static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.GetFiles(".gitignore").Length > 0)
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root");
    }

    public static string WriteTempFile(string fileName, string content)
    {
        string directory = Path.Combine(Path.GetTempPath(), "source-explorer-mcp-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(directory);

        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, content);

        return filePath;
    }
}
