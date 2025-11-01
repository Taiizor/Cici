using System.CommandLine;
using System.CommandLine.Invocation;
using Cici;
using Cici.Analyzer;
using Cici.Reporter;
using Cici.Runner;
using Spectre.Console;

var rootCommand = new RootCommand("Cici - Flaky Test Detector for .NET");

// Add ASCII art header
void PrintHeader()
{
    AnsiConsole.Write(new FigletText("CICI")
        .LeftJustified()
        .Color(Color.Cyan1));
    
    AnsiConsole.MarkupLine("[dim]Flaky Test Detector v1.0.0[/]");
    AnsiConsole.WriteLine();
}

// Run command
var runCommand = new Command("run", "Run flaky test detection on a test assembly");

var assemblyOption = new Option<FileInfo>(
    new[] { "--assembly", "-a" },
    "Path to the test assembly (.dll)") { IsRequired = true };

var filterOption = new Option<string?>(
    new[] { "--filter", "-f" },
    "Filter tests by name (partial match)");

var repeatOption = new Option<int>(
    new[] { "--repeat", "-r" },
    getDefaultValue: () => 10,
    "Number of times to run each test");

var reportOption = new Option<string[]>(
    new[] { "--report" },
    getDefaultValue: () => new[] { "console" },
    "Report formats (console, json, html)");

var outputOption = new Option<DirectoryInfo?>(
    new[] { "--output", "-o" },
    "Output directory for reports");

var parallelOption = new Option<bool>(
    new[] { "--parallel", "-p" },
    getDefaultValue: () => false,
    "Run test iterations in parallel");

runCommand.AddOption(assemblyOption);
runCommand.AddOption(filterOption);
runCommand.AddOption(repeatOption);
runCommand.AddOption(reportOption);
runCommand.AddOption(outputOption);
runCommand.AddOption(parallelOption);

runCommand.SetHandler(async (context) =>
{
    PrintHeader();
    
    var assembly = context.ParseResult.GetValueForOption(assemblyOption)!;
    var filter = context.ParseResult.GetValueForOption(filterOption);
    var repeat = context.ParseResult.GetValueForOption(repeatOption);
    var reportFormats = context.ParseResult.GetValueForOption(reportOption)!;
    var output = context.ParseResult.GetValueForOption(outputOption);
    var parallel = context.ParseResult.GetValueForOption(parallelOption);
    
    if (!assembly.Exists)
    {
        AnsiConsole.MarkupLine($"[red]Error: Assembly not found: {assembly.FullName}[/]");
        context.ExitCode = 1;
        return;
    }
    
    // Create reporters based on options
    var reporters = new List<IReporter>();
    
    foreach (var format in reportFormats)
    {
        switch (format.ToLower())
        {
            case "console":
                reporters.Add(new ConsoleReporter());
                break;
            case "json":
                var jsonReporter = new JsonReporter();
                if (output != null)
                {
                    jsonReporter.OutputPath = Path.Combine(output.FullName, "flaky-report.json");
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
    
    if (!reporters.Any())
    {
        reporters.Add(new ConsoleReporter());
    }
    
    // Configure runner options
    var options = new CiciRunOptions
    {
        AssemblyPath = assembly.FullName,
        TestFilter = filter,
        RepeatCount = repeat,
        ParallelExecution = parallel,
        OutputDirectory = output?.FullName
    };
    
    // Configure services
    var executorOptions = new TestExecutorOptions
    {
        ParallelExecution = parallel,
        TimeoutSeconds = 30,
        CollectDetailedErrors = true
    };
    
    var runner = new CiciRunner(
        discoveryService: new TestDiscoveryService(),
        executor: new TestExecutor(executorOptions),
        analyzer: new FlakyAnalyzer(),
        reporters: reporters
    );
    
    // Run the analysis
    var result = await runner.RunAsync(options);
    
    if (!result.Success)
    {
        context.ExitCode = 1;
        return;
    }
    
    // Set exit code based on flaky tests found
    if (result.FlakyTests > 0)
    {
        context.ExitCode = 2; // Warning exit code
    }
});

rootCommand.AddCommand(runCommand);

// Analyze command (for analyzing existing test results)
var analyzeCommand = new Command("analyze", "Analyze existing test result files");
analyzeCommand.SetHandler(() =>
{
    PrintHeader();
    AnsiConsole.MarkupLine("[yellow]Analyze command not yet implemented[/]");
});

rootCommand.AddCommand(analyzeCommand);

// Version command
var versionCommand = new Command("version", "Show version information");
versionCommand.SetHandler(() =>
{
    PrintHeader();
    AnsiConsole.MarkupLine("[cyan]Version:[/] 1.0.0");
    AnsiConsole.MarkupLine("[cyan]Runtime:[/] .NET 8.0");
});

rootCommand.AddCommand(versionCommand);

// Execute the command
return await rootCommand.InvokeAsync(args);
