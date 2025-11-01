using Cici.Analyzer;
using Cici.CLI.Options;
using Cici.CLI.Services;
using Cici.Reporter;
using Cici.Runner;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Cici.CLI.Commands
{
    /// <summary>
    /// Command to run flaky test detection on a test assembly.
    /// </summary>
    public class RunCommand(ILogger<RunCommand> logger, IConsoleService console, IFileSystemService fileSystem) : ICommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name => "run";

        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        public string Description => "Run flaky test detection on a test assembly";

        /// <summary>
        /// Executes the run command to analyze tests for flakiness.
        /// </summary>
        /// <param name="args">Command arguments including assembly path and options.</param>
        /// <returns>Exit code: 0 for success, 1 for error, 2 for success with flaky tests found.</returns>
        public async Task<int> ExecuteAsync(string[] args)
        {
            RunOptions? options = ParseOptions(args);

            if (options == null)
            {
                console.WriteError("Invalid arguments. Use --help for usage information.");
                return 1;
            }

            // Validate assembly path
            if (!fileSystem.FileExists(options.AssemblyPath))
            {
                console.WriteError($"Assembly not found: {options.AssemblyPath}");
                return 1;
            }

            try
            {
                logger.LogInformation("Starting flaky test detection for {Assembly}", options.AssemblyPath);

                // Show progress
                CiciRunResult result = await AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(),
                    })
                    .StartAsync(async ctx =>
                    {
                        ProgressTask task = ctx.AddTask("[cyan]Analyzing tests...[/]");
                        return await RunAnalysisAsync(options, task);
                    });

                if (result.Success)
                {
                    logger.LogInformation("Analysis completed successfully. Found {FlakyCount} flaky tests", result.FlakyTests);
                    return result.FlakyTests > 0 ? 2 : 0; // Return 2 if flaky tests found (warning)
                }
                else
                {
                    console.WriteError($"Analysis failed: {result.ErrorMessage}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during test analysis");
                console.WriteError($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Parses command line arguments into run options.
        /// </summary>
        /// <param name="args">Command line arguments to parse.</param>
        /// <returns>Parsed options or null if parsing fails.</returns>
        private RunOptions? ParseOptions(string[] args)
        {
            RunOptions options = new();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-a":
                    case "--assembly":
                        if (i + 1 < args.Length)
                        {
                            options.AssemblyPath = args[++i];
                        }

                        break;

                    case "-f":
                    case "--filter":
                        if (i + 1 < args.Length)
                        {
                            options.Filter = args[++i];
                        }

                        break;

                    case "-r":
                    case "--repeat":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int r))
                        {
                            options.RepeatCount = r;
                        }

                        break;

                    case "--report":
                        if (i + 1 < args.Length)
                        {
                            options.ReportFormats.Clear();
                            options.ReportFormats.AddRange(args[++i].Split(','));
                        }
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length)
                        {
                            options.OutputDirectory = args[++i];
                        }

                        break;

                    case "-p":
                    case "--parallel":
                        options.Parallel = true;
                        break;

                    case "--timeout":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int t))
                        {
                            options.TimeoutSeconds = t;
                        }

                        break;

                    case "-v":
                    case "--verbose":
                        options.Verbose = true;
                        break;
                }
            }

            // Validate required options
            if (string.IsNullOrEmpty(options.AssemblyPath))
            {
                console.WriteError("Assembly path is required. Use -a or --assembly option.");
                return null;
            }

            return options;
        }

        /// <summary>
        /// Runs the flaky test analysis with progress tracking.
        /// </summary>
        /// <param name="options">Run configuration options.</param>
        /// <param name="progressTask">Progress task for updating UI.</param>
        /// <returns>The analysis results.</returns>
        private async Task<CiciRunResult> RunAnalysisAsync(RunOptions options, ProgressTask progressTask)
        {
            // Create reporters based on options
            List<IReporter> reporters = CreateReporters(options);

            // Configure services
            TestExecutorOptions executorOptions = new()
            {
                ParallelExecution = options.Parallel,
                TimeoutSeconds = options.TimeoutSeconds,
                CollectDetailedErrors = true
            };

            CiciRunner runner = new(
                discoveryService: new TestDiscoveryService(),
                executor: new TestExecutor(executorOptions),
                analyzer: new FlakyAnalyzer(),
                reporters: reporters
            );

            // Create run options
            CiciRunOptions runOptions = new()
            {
                AssemblyPath = options.AssemblyPath,
                TestFilter = options.Filter,
                RepeatCount = options.RepeatCount,
                ParallelExecution = options.Parallel,
                OutputDirectory = options.OutputDirectory
            };

            // Create progress reporter that updates the Spectre.Console progress bar
            Progress<double> progress = new(percentage =>
            {
                // Update the progress task to the reported percentage
                progressTask.Value = percentage;
            });

            // Run the analysis with progress tracking
            CiciRunResult result = await runner.RunAsync(runOptions, progress);

            // Ensure progress is at 100%
            progressTask.Value = 100;

            return result;
        }

        /// <summary>
        /// Creates reporter instances based on the specified report formats.
        /// </summary>
        /// <param name="options">Run options containing report format specifications.</param>
        /// <returns>List of configured reporters.</returns>
        private List<IReporter> CreateReporters(RunOptions options)
        {
            List<IReporter> reporters = [];

            foreach (string format in options.ReportFormats)
            {
                switch (format.ToLowerInvariant())
                {
                    case "console":
                        reporters.Add(new ConsoleReporter());
                        break;

                    case "json":
                        JsonReporter jsonReporter = new();
                        if (!string.IsNullOrEmpty(options.OutputDirectory))
                        {
                            fileSystem.EnsureDirectoryExists(options.OutputDirectory);
                            jsonReporter.OutputPath = Path.Combine(options.OutputDirectory, "flaky-report.json");
                        }
                        reporters.Add(jsonReporter);
                        break;

                    case "html":
                        console.WriteWarning("HTML reporter is not yet implemented.");
                        break;

                    default:
                        console.WriteWarning($"Unknown report format: {format}");
                        break;
                }
            }

            if (reporters.Count == 0)
            {
                reporters.Add(new ConsoleReporter());
            }

            return reporters;
        }
    }
}