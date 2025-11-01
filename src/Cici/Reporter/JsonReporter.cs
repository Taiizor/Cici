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
        /// <summary>
        /// Gets or sets the total number of tests analyzed.
        /// </summary>
        public int TotalTests { get; set; }
        
        /// <summary>
        /// Gets or sets the number of stable tests (consistently pass or consistently fail).
        /// </summary>
        public int StableTests { get; set; }
        
        /// <summary>
        /// Gets or sets the number of flaky tests detected.
        /// </summary>
        public int FlakyTests { get; set; }
        
        /// <summary>
        /// Gets or sets the number of tests that always fail.
        /// </summary>
        public int AlwaysFailingTests { get; set; }
        
        /// <summary>
        /// Gets or sets the overall flaky rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double OverallFlakyRate { get; set; }
        
        /// <summary>
        /// Gets or sets the total time spent executing all tests.
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }
        
        /// <summary>
        /// Gets or sets the error patterns found across all tests with occurrence counts.
        /// </summary>
        public Dictionary<string, int> ErrorPatterns { get; set; } = [];
    }

    /// <summary>
    /// Individual test result for the JSON report.
    /// </summary>
    public class JsonTestResult
    {
        /// <summary>
        /// Gets or sets the fully qualified name of the test.
        /// </summary>
        public string TestName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the class name containing the test.
        /// </summary>
        public string ClassName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the test method name.
        /// </summary>
        public string MethodName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the test framework name.
        /// </summary>
        public string Framework { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the total number of test runs.
        /// </summary>
        public int TotalRuns { get; set; }
        
        /// <summary>
        /// Gets or sets the number of passing runs.
        /// </summary>
        public int PassedCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of failing runs.
        /// </summary>
        public int FailedCount { get; set; }
        
        /// <summary>
        /// Gets or sets the pass rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double PassRate { get; set; }
        
        /// <summary>
        /// Gets or sets whether the test is flaky.
        /// </summary>
        public bool IsFlaky { get; set; }
        
        /// <summary>
        /// Gets or sets the average duration across all runs.
        /// </summary>
        public TimeSpan AverageDuration { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum duration across all runs.
        /// </summary>
        public TimeSpan MinDuration { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum duration across all runs.
        /// </summary>
        public TimeSpan MaxDuration { get; set; }
        
        /// <summary>
        /// Gets or sets the detailed execution results for each run.
        /// </summary>
        public List<JsonExecution> Executions { get; set; } = [];
    }

    /// <summary>
    /// Represents the result of a single JSON-based execution attempt, including status, timing, and error details.
    /// </summary>
    /// <remarks>This class encapsulates information about an individual execution, such as whether it
    /// succeeded, when it occurred, how long it took, and any error information if the attempt failed. It is typically
    /// used to record or analyze the outcome of JSON-driven operations, such as tests or jobs.</remarks>
    /// <summary>
    /// Individual test execution data for the JSON report.
    /// </summary>
    public class JsonExecution
    {
        /// <summary>
        /// Gets or sets the attempt number for this execution.
        /// </summary>
        public int Attempt { get; set; }
        
        /// <summary>
        /// Gets or sets whether the test passed in this execution.
        /// </summary>
        public bool Passed { get; set; }
        
        /// <summary>
        /// Gets or sets the duration of the test execution.
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the test was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the error message if the test failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the stack trace if the test failed.
        /// </summary>
        public string? StackTrace { get; set; }
    }
}