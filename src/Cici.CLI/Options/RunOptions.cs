namespace Cici.CLI.Options;

public class RunOptions
{
    public string AssemblyPath { get; set; } = string.Empty;
    public string? Filter { get; set; }
    public int RepeatCount { get; set; } = 10;
    public List<string> ReportFormats { get; set; } = new() { "console" };
    public string? OutputDirectory { get; set; }
    public bool Parallel { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool Verbose { get; set; }
}
