using System.Text.Json;
using System.Text.Json.Serialization;
using Cici.Analyzer;
using Cici.Models;

namespace Cici.Reporter;

public class JsonReporter : IFileReporter
{
    public string OutputPath { get; set; } = "flaky-report.json";

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task GenerateReportAsync(IEnumerable<FlakyTestResult> results, FlakyDetectionSummary summary)
    {
        var report = new JsonReport
        {
            GeneratedAt = DateTime.Now,
            Summary = new JsonSummary
            {
                TotalTests = summary.TotalTests,
                StableTests = summary.StableTests,
                FlakyTests = summary.FlakyTests,
                AlwaysFailingTests = summary.AlwaysFailingTests,
                OverallFlakyRate = summary.OverallFlakyRate,
                TotalExecutionTime = summary.TotalExecutionTime,
                ErrorPatterns = summary.ErrorPatterns
            },
            Tests = results.Select(r => new JsonTestResult
            {
                TestName = r.Test.FullName,
                ClassName = r.Test.ClassName,
                MethodName = r.Test.MethodName,
                Framework = r.Test.Framework.ToString(),
                TotalRuns = r.TotalRuns,
                PassedCount = r.PassedCount,
                FailedCount = r.FailedCount,
                PassRate = r.PassRate,
                IsFlaky = r.IsFlaky,
                AverageDuration = r.AverageDuration,
                MinDuration = r.MinDuration,
                MaxDuration = r.MaxDuration,
                Executions = r.ExecutionResults.Select(e => new JsonExecution
                {
                    Attempt = e.Attempt,
                    Passed = e.Passed,
                    Duration = e.Duration,
                    ExecutedAt = e.ExecutedAt,
                    ErrorMessage = e.ErrorMessage,
                    StackTrace = e.StackTrace
                }).ToList()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(report, _options);
        await File.WriteAllTextAsync(OutputPath, json);
    }
}

public class JsonReport
{
    public DateTime GeneratedAt { get; set; }
    public JsonSummary Summary { get; set; } = null!;
    public List<JsonTestResult> Tests { get; set; } = new();
}

public class JsonSummary
{
    public int TotalTests { get; set; }
    public int StableTests { get; set; }
    public int FlakyTests { get; set; }
    public int AlwaysFailingTests { get; set; }
    public double OverallFlakyRate { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public Dictionary<string, int> ErrorPatterns { get; set; } = new();
}

public class JsonTestResult
{
    public string TestName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public int TotalRuns { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public double PassRate { get; set; }
    public bool IsFlaky { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public List<JsonExecution> Executions { get; set; } = new();
}

public class JsonExecution
{
    public int Attempt { get; set; }
    public bool Passed { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}
