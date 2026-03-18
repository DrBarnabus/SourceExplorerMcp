using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SourceExplorerMcp.Core.Services;
using SourceExplorerMcp.Tools;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Debug()
    .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
    .WriteTo.File(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "server_.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting server...");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();

    builder.Services.AddMemoryCache();

    builder.Services.AddSingleton<IProjectAssetsParser, ProjectAssetsParser>();
    builder.Services.AddSingleton<IAssemblyMetadataExtractor, AssemblyMetadataExtractor>();
    builder.Services.AddSingleton<IRuntimeAssemblyResolver, RuntimeAssemblyResolver>();
    builder.Services.AddSingleton<IAssemblyDiscoveryService, AssemblyDiscoveryService>();
    builder.Services.AddSingleton<ITypeSearchService, TypeSearchService>();
    builder.Services.AddSingleton<IDecompilerService, DecompilerService>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<SearchTypesTool>()
        .WithTools<DecompileTypeTool>();

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
