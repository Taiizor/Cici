using Cici.CLI.Commands;
using Cici.CLI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Text;

namespace Cici.CLI
{
    /// <summary>
    /// Entry point for the Cici CLI application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the CLI application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code: 0 for success, non-zero for failure.</returns>
        public static async Task<int> Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            // Configure Serilog
            ConfigureLogging(args);

            try
            {
                Log.Information("Starting Cici CLI");

                IHost host = CreateHostBuilder(args).Build();
                CommandExecutor executor = host.Services.GetRequiredService<CommandExecutor>();

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

        /// <summary>
        /// Creates and configures the host builder with dependency injection and configuration.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Configured host builder.</returns>
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
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
        }

        /// <summary>
        /// Configures Serilog logging based on command line arguments and configuration.
        /// </summary>
        /// <param name="args">Command line arguments to check for verbose flag.</param>
        private static void ConfigureLogging(string[] args)
        {
            LogEventLevel logLevel = args.Contains("-v") || args.Contains("--verbose")
                ? LogEventLevel.Debug
                : LogEventLevel.Information;

            string logPath = Path.Combine(
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
}