using Spectre.Console;

namespace Cici.CLI.Services;

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
        
        AnsiConsole.MarkupLine("[dim]Flaky Test Detector v1.0.1[/]");
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
