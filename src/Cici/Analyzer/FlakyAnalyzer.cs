using Cici.Models;
using System.Text.RegularExpressions;

namespace Cici.Analyzer
{
    /// <summary>
    /// Provides flaky test detection and analysis capabilities by examining test execution patterns.
    /// </summary>
    public class FlakyAnalyzer(FlakyAnalyzerOptions? options = null) : IFlakyAnalyzer
    {
        private readonly FlakyAnalyzerOptions _options = options ?? new FlakyAnalyzerOptions();

        /// <summary>
        /// Analyzes the execution results of a single test to determine if it's flaky.
        /// </summary>
        /// <param name="test">The test being analyzed.</param>
        /// <param name="executionResults">Collection of execution results from multiple test runs.</param>
        /// <returns>A comprehensive analysis result including pass rate, duration statistics, and flakiness determination.</returns>
        public FlakyTestResult AnalyzeTestResults(TestInfo test, List<TestExecutionResult> executionResults)
        {
            FlakyTestResult result = new()
            {
                Test = test,
                ExecutionResults = executionResults,
                TotalRuns = executionResults.Count,
                PassedCount = executionResults.Count(r => r.Passed),
                FailedCount = executionResults.Count(r => !r.Passed)
            };

            if (executionResults.Any())
            {
                List<TimeSpan> durations = executionResults.Select(r => r.Duration).ToList();
                result.AverageDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
                result.MinDuration = durations.Min();
                result.MaxDuration = durations.Max();
            }

            return result;
        }

        /// <summary>
        /// Analyzes multiple tests in batch for improved performance.
        /// </summary>
        /// <param name="testResults">Dictionary mapping test information to their execution results.</param>
        /// <returns>Collection of analyzed results for each test.</returns>
        public IEnumerable<FlakyTestResult> AnalyzeBatch(Dictionary<TestInfo, List<TestExecutionResult>> testResults)
        {
            List<FlakyTestResult> results = [];

            foreach (KeyValuePair<TestInfo, List<TestExecutionResult>> kvp in testResults)
            {
                FlakyTestResult result = AnalyzeTestResults(kvp.Key, kvp.Value);
                results.Add(result);
            }

            return results.OrderByDescending(r => r.IsFlaky)
                          .ThenBy(r => r.PassRate);
        }

        /// <summary>
        /// Generates a comprehensive summary report from analyzed test results.
        /// </summary>
        /// <param name="results">Collection of analyzed test results.</param>
        /// <returns>Summary containing statistics, patterns, and insights about test flakiness.</returns>
        public FlakyDetectionSummary GenerateSummary(IEnumerable<FlakyTestResult> results)
        {
            List<FlakyTestResult> resultsList = results.ToList();

            FlakyDetectionSummary summary = new()
            {
                TotalTests = resultsList.Count,
                FlakyTests = resultsList.Count(r => r.IsFlaky),
                StableTests = resultsList.Count(r => !r.IsFlaky && r.PassRate == 1.0),
                AlwaysFailingTests = resultsList.Count(r => r.PassRate == 0),
                // Get most flaky tests
                MostFlakyTests = resultsList
                    .Where(r => r.IsFlaky)
                    .OrderBy(r => Math.Abs(r.PassRate - 0.5)) // Tests closer to 50% pass rate are most unpredictable
                    .Take(_options.TopFlakyTestsToReport)
                    .ToList(),

                // Analyze error patterns
                ErrorPatterns = AnalyzeErrorPatterns(resultsList),

                // Calculate total execution time
                TotalExecutionTime = TimeSpan.FromMilliseconds(
                    resultsList.SelectMany(r => r.ExecutionResults)
                              .Sum(e => e.Duration.TotalMilliseconds))
            };

            return summary;
        }

        /// <summary>
        /// Analyzes error patterns from a collection of test results to identify common trends.
        /// </summary>
        /// <param name="results">Collection of test results to analyze.</param>
        /// <returns>A dictionary mapping error patterns to their frequency.</returns>
        private Dictionary<string, int> AnalyzeErrorPatterns(List<FlakyTestResult> results)
        {
            Dictionary<string, int> errorPatterns = [];
            IEnumerable<TestExecutionResult> failedExecutions = results.SelectMany(r => r.ExecutionResults)
                                          .Where(e => !e.Passed && !string.IsNullOrEmpty(e.ErrorMessage));

            foreach (TestExecutionResult? execution in failedExecutions)
            {
                string pattern = CategorizeError(execution.ErrorMessage!);

                if (!errorPatterns.ContainsKey(pattern))
                {
                    errorPatterns[pattern] = 0;
                }

                errorPatterns[pattern]++;
            }

            return errorPatterns.OrderByDescending(kvp => kvp.Value)
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Categorizes error messages into common patterns for trend analysis.
        /// </summary>
        /// <param name="errorMessage">The error message to categorize.</param>
        /// <returns>A category name representing the type of error.</returns>
        private string CategorizeError(string? errorMessage)
        {
            // Common error patterns in flaky tests
            Dictionary<string, string> patterns = new()
            {
                { @"timeout|timed out", "Timeout" },
                { @"connection|socket|network", "Network/Connection Issue" },
                { @"access denied|permission|unauthorized", "Permission Issue" },
                { @"null reference|nullpointer", "Null Reference" },
                { @"assertion failed|assert\.", "Assertion Failure" },
                { @"concurrent|thread|task.*cancel", "Concurrency Issue" },
                { @"file.*not found|path.*not exist", "File/Path Issue" },
                { @"out of memory|memory", "Memory Issue" },
                { @"deadlock|lock", "Deadlock/Lock Issue" }
            };

            foreach (KeyValuePair<string, string> pattern in patterns)
            {
                if (Regex.IsMatch(errorMessage ?? string.Empty, pattern.Key, RegexOptions.IgnoreCase))
                {
                    return pattern.Value;
                }
            }

            // If no pattern matches, try to extract the exception type
            Match exceptionMatch = Regex.Match(errorMessage ?? string.Empty, @"(\w+Exception)");
            if (exceptionMatch.Success)
            {
                return exceptionMatch.Groups[1].Value;
            }

            return "Other";
        }
    }

    /// <summary>
    /// Configuration options for the flaky test analyzer.
    /// </summary>
    public class FlakyAnalyzerOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of flaky tests to include in the summary report. Default is 10.
        /// </summary>
        public int TopFlakyTestsToReport { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to analyze and categorize error patterns. Default is true.
        /// </summary>
        public bool AnalyzeErrorPatterns { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold for considering a test flaky. Tests failing at least this percentage of the time are considered flaky. Default is 0.1 (10%).
        /// </summary>
        public double FlakyThreshold { get; set; } = 0.1; // Consider test flaky if it fails at least 10% of the time
    }
}