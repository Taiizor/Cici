using Cici.Analyzer;
using Cici.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cici.Reporter
{
    /// <summary>
    /// Generates flaky test reports in JSON format for programmatic consumption.
    /// </summary>
    public class JsonReporter : IFileReporter
    {
        /// <summary>
        /// Gets or sets the output file path for the JSON report. Default is "flaky-report.json".
        /// </summary>
        public string OutputPath { get; set; } = "flaky-report.json";

        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Generates a JSON report from the test analysis results and saves it to a file.
        /// </summary>
        /// <param name="results">Collection of analyzed test results.</param>
        /// <param name="summary">Summary statistics from the analysis.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateReportAsync(IEnumerable<FlakyTestResult> results, FlakyDetectionSummary summary)
        {
            JsonReport report = new()
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

            string json = JsonSerializer.Serialize(report, _options);
            await File.WriteAllTextAsync(OutputPath, json);
        }
    }

    /// <summary>
    /// Root object for the JSON report.
    /// </summary>
    public class JsonReport
    {
        /// <summary>
        /// Gets or sets the timestamp when the report was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Gets or sets the summary statistics.
        /// </summary>
        public JsonSummary Summary { get; set; } = new();

        /// <summary>
        /// Gets or sets the detailed test results.
        /// </summary>
        public List<JsonTestResult> Tests { get; set; } = [];
    }

    /// <summary>
    /// Summary statistics for the JSON report.
    /// </summary>
    public class JsonSummary
    {
        public int TotalTests { get; set; }
        public int StableTests { get; set; }
        public int FlakyTests { get; set; }
        public int AlwaysFailingTests { get; set; }
        public double OverallFlakyRate { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public Dictionary<string, int> ErrorPatterns { get; set; } = [];
    }

    /// <summary>
    /// Individual test result for the JSON report.
    /// </summary>
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
        public List<JsonExecution> Executions { get; set; } = [];
    }

    /// <summary>
    /// Represents the result of a single JSON-based execution attempt, including status, timing, and error details.
    /// </summary>
    /// <remarks>This class encapsulates information about an individual execution, such as whether it
    /// succeeded, when it occurred, how long it took, and any error information if the attempt failed. It is typically
    /// used to record or analyze the outcome of JSON-driven operations, such as tests or jobs.</remarks>
    public class JsonExecution
    {
        public int Attempt { get; set; }
        public bool Passed { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}