using Cici.CLI.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Reflection;

namespace Cici.CLI.Commands
{
    /// <summary>
    /// Command to display version and system information.
    /// </summary>
    public class VersionCommand(ILogger<VersionCommand> logger, IConsoleService console) : ICommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name => "version";

        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        public string Description => "Show version information";

        /// <summary>
        /// Executes the version command, displaying version and system information.
        /// </summary>
        /// <param name="args">Command arguments (unused).</param>
        /// <returns>Exit code (always 0 for success).</returns>
        public Task<int> ExecuteAsync(string[] args)
        {
            console.WriteHeader();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString()
                          ?? "1.0.0";

            Table table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[cyan]Property[/]").LeftAligned())
                .AddColumn(new TableColumn("[white]Value[/]").LeftAligned());

            table.AddRow("Version", version);
            table.AddRow("Runtime", $".NET {Environment.Version}");
            table.AddRow("OS", Environment.OSVersion.ToString());
            table.AddRow("Architecture", Environment.Is64BitProcess ? "x64" : "x86");
            table.AddRow("Machine", Environment.MachineName);

            AnsiConsole.Write(table);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]For more information, visit: https://github.com/Taiizor/Cici[/]");

            logger.LogInformation("Version information displayed");

            return Task.FromResult(0);
        }
    }
}