using Cici.CLI.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Cici.CLI.Commands
{
    public class HelpCommand(ILogger<HelpCommand> logger, IConsoleService console) : ICommand
    {
        public string Name => "help";
        public string Description => "Show help and usage information";

        public Task<int> ExecuteAsync(string[] args)
        {
            console.WriteHeader();

            AnsiConsole.MarkupLine("[bold cyan]Usage:[/] cici <command> [[options]]");
            AnsiConsole.WriteLine();

            // Commands table
            AnsiConsole.MarkupLine("[bold cyan]Commands:[/]");
            Table commandTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").PadRight(2))
                .AddColumn(new TableColumn("").PadRight(20))
                .AddColumn(new TableColumn(""));

            // Static command list
            commandTable.AddRow("", "[yellow]run[/]", "Run flaky test detection on a test assembly");
            commandTable.AddRow("", "[yellow]version[/]", "Show version information");
            commandTable.AddRow("", "[yellow]help[/]", "Show help and usage information");

            AnsiConsole.Write(commandTable);
            AnsiConsole.WriteLine();

            // Run command specific options
            AnsiConsole.MarkupLine("[bold cyan]Run Command Options:[/]");
            Table optionsTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").PadRight(2))
                .AddColumn(new TableColumn("").PadRight(30))
                .AddColumn(new TableColumn(""));

            optionsTable.AddRow("", "[green]-a, --assembly[/] <path>", "Path to the test assembly (.dll) [bold red][[required]][/]");
            optionsTable.AddRow("", "[green]-f, --filter[/] <filter>", "Filter tests by name (partial match)");
            optionsTable.AddRow("", "[green]-r, --repeat[/] <count>", "Number of times to run each test [dim](default: 10)[/]");
            optionsTable.AddRow("", "[green]--report[/] <formats>", "Report formats: console, json [dim](default: console)[/]");
            optionsTable.AddRow("", "[green]-o, --output[/] <dir>", "Output directory for reports");
            optionsTable.AddRow("", "[green]-p, --parallel[/]", "Run test iterations in parallel");
            optionsTable.AddRow("", "[green]--timeout[/] <seconds>", "Test execution timeout [dim](default: 30)[/]");
            optionsTable.AddRow("", "[green]-v, --verbose[/]", "Enable verbose logging");
            optionsTable.AddRow("", "[green]-h, --help[/]", "Show help and usage information");

            AnsiConsole.Write(optionsTable);
            AnsiConsole.WriteLine();

            // Examples
            AnsiConsole.MarkupLine("[bold cyan]Examples:[/]");
            AnsiConsole.MarkupLine("  [dim]# Basic usage[/]");
            AnsiConsole.MarkupLine("  cici run -a MyTests.dll");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [dim]# Run each test 20 times with parallel execution[/]");
            AnsiConsole.MarkupLine("  cici run -a MyTests.dll -r 20 -p");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [dim]# Filter specific tests and generate JSON report[/]");
            AnsiConsole.MarkupLine("  cici run -a MyTests.dll -f \"UserTests\" --report json -o ./reports");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[dim]For more information, visit: https://github.com/Taiizor/Cici[/]");

            logger.LogInformation("Help information displayed");

            return Task.FromResult(0);
        }
    }
}