using Spectre.Console;
using System.Reflection;

namespace Cici.CLI.Services
{
    /// <summary>
    /// Provides console interaction services with formatted, colored output using Spectre.Console.
    /// </summary>
    public class ConsoleService : IConsoleService
    {
        /// <summary>
        /// Writes an informational message with cyan info icon.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void WriteInfo(string message)
        {
            AnsiConsole.MarkupLine($"[cyan]ℹ[/] {message}");
        }

        /// <summary>
        /// Writes a success message with green checkmark.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        public void WriteSuccess(string message)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {message}");
        }

        /// <summary>
        /// Writes a warning message with yellow warning icon.
        /// </summary>
        /// <param name="message">The warning message to display.</param>
        public void WriteWarning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");
        }

        /// <summary>
        /// Writes an error message with red error icon.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public void WriteError(string message)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {message}");
        }

        /// <summary>
        /// Writes the application header with ASCII art and version information.
        /// </summary>
        public void WriteHeader()
        {
            AnsiConsole.Write(new FigletText("CICI")
                .LeftJustified()
                .Color(Color.Cyan1));

            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString()
                          ?? "1.0.0";

            AnsiConsole.MarkupLine($"[dim]Flaky Test Detector v{version}[/]");
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Prompts the user for input with type conversion.
        /// </summary>
        /// <typeparam name="T">The expected type of the input.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <returns>The user's input converted to type T.</returns>
        public T Prompt<T>(string message)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<T>(message)
                    .PromptStyle("cyan"));
        }

        /// <summary>
        /// Displays a yes/no confirmation prompt.
        /// </summary>
        /// <param name="message">The confirmation message.</param>
        /// <returns>True if user confirms; otherwise, false.</returns>
        public bool Confirm(string message)
        {
            return AnsiConsole.Confirm(message);
        }
    }
}