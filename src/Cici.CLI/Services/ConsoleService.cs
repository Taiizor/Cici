using Spectre.Console;
using System.Reflection;

namespace Cici.CLI.Services
{
    public class ConsoleService : IConsoleService
    {
        public void WriteInfo(string message)
        {
            AnsiConsole.MarkupLine($"[cyan]ℹ[/] {message}");
        }

        public void WriteSuccess(string message)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {message}");
        }

        public void WriteWarning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");
        }

        public void WriteError(string message)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {message}");
        }

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

        public T Prompt<T>(string message)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<T>(message)
                    .PromptStyle("cyan"));
        }

        public bool Confirm(string message)
        {
            return AnsiConsole.Confirm(message);
        }
    }
}