using Cici.Analyzer;
using Cici.Reporter;
using Cici.Runner;
using Spectre.Console;

namespace Cici.CLI
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Add ASCII art header
            void PrintHeader()
            {
                AnsiConsole.Write(new FigletText("CICI")
                    .LeftJustified()
                    .Color(Color.Cyan1));

                AnsiConsole.MarkupLine("[dim]Flaky Test Detector v1.0.1[/]");
                AnsiConsole.WriteLine();
            }

            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintHeader();
                Console.WriteLine("Usage: cici run --assembly <path> [options]");
                Console.WriteLine();
                Console.WriteLine("Commands:");
                Console.WriteLine("  run                       Run flaky test detection on a test assembly");
                Console.WriteLine("  version                   Show version information");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -a, --assembly <path>     Path to the test assembly (.dll) [required]");
                Console.WriteLine("  -f, --filter <filter>     Filter tests by name (partial match)");
                Console.WriteLine("  -r, --repeat <count>      Number of times to run each test (default: 10)");
                Console.WriteLine("  --report <formats>        Report formats: console, json (default: console)");
                Console.WriteLine("  -o, --output <dir>        Output directory for reports");
                Console.WriteLine("  -p, --parallel            Run test iterations in parallel");
                Console.WriteLine("  -h, --help                Show help and usage information");
                return 0;
            }

            if (args[0] == "version")
            {
                PrintHeader();
                AnsiConsole.MarkupLine("[cyan]Version:[/] 1.0.1");
                AnsiConsole.MarkupLine("[cyan]Runtime:[/] .NET 8.0");
                return 0;
            }

            if (args[0] == "run")
            {
                // Parse command line arguments
                string? assemblyPath = null;
                string? filter = null;
                int repeat = 10;
                List<string> reportFormats = ["console"];
                string? outputDir = null;
                bool parallel = false;

                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-a":
                        case "--assembly":
                            if (i + 1 < args.Length)
                            {
                                assemblyPath = args[++i];
                            }

                            break;

                        case "-f":
                        case "--filter":
                            if (i + 1 < args.Length)
                            {
                                filter = args[++i];
                            }

                            break;

                        case "-r":
                        case "--repeat":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int r))
                            {
                                repeat = r;
                            }

                            break;

                        case "--report":
                            if (i + 1 < args.Length)
                            {
                                reportFormats.Clear();
                                reportFormats.AddRange(args[++i].Split(','));
                            }
                            break;

                        case "-o":
                        case "--output":
                            if (i + 1 < args.Length)
                            {
                                outputDir = args[++i];
                            }

                            break;

                        case "-p":
                        case "--parallel":
                            parallel = true;
                            break;
                    }
                }

                // Validate required arguments
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    AnsiConsole.MarkupLine("[red]Error: Assembly path is required. Use -a or --assembly option.[/]");
                    return 1;
                }

                if (!File.Exists(assemblyPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error: Assembly not found: {assemblyPath}[/]");
                    return 1;
                }

                await RunAnalysis(assemblyPath, filter, repeat, reportFormats, outputDir, parallel);
                return 0;
            }

            AnsiConsole.MarkupLine("[red]Unknown command. Use --help for usage information.[/]");
            return 1;

            async Task RunAnalysis(string assemblyPath, string? filter, int repeat, List<string> reportFormats, string? outputDir, bool parallel)
            {
                PrintHeader();

                // Create reporters based on options
                List<IReporter> reporters = [];

                foreach (string format in reportFormats)
                {
                    switch (format.ToLowerInvariant())
                    {
                        case "console":
                            reporters.Add(new ConsoleReporter());
                            break;
                        case "json":
                            JsonReporter jsonReporter = new();
                            if (outputDir != null)
                            {
                                jsonReporter.OutputPath = Path.Combine(outputDir, "flaky-report.json");
                            }
                            reporters.Add(jsonReporter);
                            break;
                        case "html":
                            // HTML reporter would go here when implemented
                            AnsiConsole.MarkupLine("[yellow]HTML reporter not yet implemented[/]");
                            break;
                        default:
                            AnsiConsole.MarkupLine($"[yellow]Unknown report format: {format}[/]");
                            break;
                    }
                }

                if (reporters.Count == 0)
                {
                    reporters.Add(new ConsoleReporter());
                }

                // Configure runner options
                CiciRunOptions options = new()
                {
                    AssemblyPath = assemblyPath,
                    TestFilter = filter,
                    RepeatCount = repeat,
                    ParallelExecution = parallel,
                    OutputDirectory = outputDir
                };

                // Configure services
                TestExecutorOptions executorOptions = new()
                {
                    ParallelExecution = parallel,
                    TimeoutSeconds = 30,
                    CollectDetailedErrors = true
                };

                CiciRunner runner = new(
                    discoveryService: new TestDiscoveryService(),
                    executor: new TestExecutor(executorOptions),
                    analyzer: new FlakyAnalyzer(),
                    reporters: reporters
                );

                // Run the analysis
                var result = await runner.RunAsync(options);

                if (!result.Success)
                {
                    Environment.Exit(1);
                    return;
                }

                // Set exit code based on flaky tests found
                if (result.FlakyTests > 0)
                {
                    Environment.Exit(2); // Warning exit code
                }
            }
        }
    }
}