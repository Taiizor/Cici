using Cici.CLI.Commands;
using Cici.CLI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Cici.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        ConfigureLogging(args);
        
        try
        {
            Log.Information("Starting Cici CLI");
            
            var host = CreateHostBuilder(args).Build();
            var executor = host.Services.GetRequiredService<CommandExecutor>();
            
            return await executor.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables("CICI_");
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<IConsoleService, ConsoleService>();
                services.AddSingleton<IFileSystemService, FileSystemService>();
                services.AddSingleton<CommandExecutor>();
                
                // Register commands
                services.AddTransient<ICommand, RunCommand>();
                services.AddTransient<ICommand, VersionCommand>();
                services.AddTransient<ICommand, HelpCommand>();
                
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });
            });
    
    private static void ConfigureLogging(string[] args)
    {
        var logLevel = args.Contains("-v") || args.Contains("--verbose") 
            ? LogEventLevel.Debug 
            : LogEventLevel.Information;
        
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "Cici", 
            "logs");
        
        Directory.CreateDirectory(logPath);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.File(
                Path.Combine(logPath, "cici-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(
                outputTemplate: logLevel == LogEventLevel.Debug 
                    ? "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    : "{Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: logLevel)
            .CreateLogger();
    }
}
