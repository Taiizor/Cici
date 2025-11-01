using System.Text.RegularExpressions;
using Cici.Models;

namespace Cici.Analyzer;

public class FlakyAnalyzer : IFlakyAnalyzer
{
    private readonly FlakyAnalyzerOptions _options;

    public FlakyAnalyzer(FlakyAnalyzerOptions? options = null)
    {
        _options = options ?? new FlakyAnalyzerOptions();
    }

    public FlakyTestResult AnalyzeTestResults(TestInfo test, List<TestExecutionResult> executionResults)
    {
        var result = new FlakyTestResult
        {
            Test = test,
            ExecutionResults = executionResults,
            TotalRuns = executionResults.Count,
            PassedCount = executionResults.Count(r => r.Passed),
            FailedCount = executionResults.Count(r => !r.Passed)
        };

        if (executionResults.Any())
        {
            var durations = executionResults.Select(r => r.Duration).ToList();
            result.AverageDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
            result.MinDuration = durations.Min();
            result.MaxDuration = durations.Max();
        }

        return result;
    }

    public IEnumerable<FlakyTestResult> AnalyzeBatch(Dictionary<TestInfo, List<TestExecutionResult>> testResults)
    {
        var results = new List<FlakyTestResult>();

        foreach (var kvp in testResults)
        {
            var result = AnalyzeTestResults(kvp.Key, kvp.Value);
            results.Add(result);
        }

        return results.OrderByDescending(r => r.IsFlaky)
                      .ThenBy(r => r.PassRate);
    }

    public FlakyDetectionSummary GenerateSummary(IEnumerable<FlakyTestResult> results)
    {
        var resultsList = results.ToList();
        
        var summary = new FlakyDetectionSummary
        {
            TotalTests = resultsList.Count,
            FlakyTests = resultsList.Count(r => r.IsFlaky),
            StableTests = resultsList.Count(r => !r.IsFlaky && r.PassRate == 1.0),
            AlwaysFailingTests = resultsList.Count(r => r.PassRate == 0)
        };

        // Get most flaky tests
        summary.MostFlakyTests = resultsList
            .Where(r => r.IsFlaky)
            .OrderBy(r => Math.Abs(r.PassRate - 0.5)) // Tests closer to 50% pass rate are most unpredictable
            .Take(_options.TopFlakyTestsToReport)
            .ToList();

        // Analyze error patterns
        summary.ErrorPatterns = AnalyzeErrorPatterns(resultsList);

        // Calculate total execution time
        summary.TotalExecutionTime = TimeSpan.FromMilliseconds(
            resultsList.SelectMany(r => r.ExecutionResults)
                      .Sum(e => e.Duration.TotalMilliseconds));

        return summary;
    }

    private Dictionary<string, int> AnalyzeErrorPatterns(List<FlakyTestResult> results)
    {
        var errorPatterns = new Dictionary<string, int>();
        var failedExecutions = results.SelectMany(r => r.ExecutionResults)
                                      .Where(e => !e.Passed && !string.IsNullOrEmpty(e.ErrorMessage));

        foreach (var execution in failedExecutions)
        {
            var pattern = CategorizeError(execution.ErrorMessage!);
            
            if (!errorPatterns.ContainsKey(pattern))
                errorPatterns[pattern] = 0;
            
            errorPatterns[pattern]++;
        }

        return errorPatterns.OrderByDescending(kvp => kvp.Value)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private string CategorizeError(string errorMessage)
    {
        // Common error patterns in flaky tests
        var patterns = new Dictionary<string, string>
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

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(errorMessage, pattern.Key, RegexOptions.IgnoreCase))
            {
                return pattern.Value;
            }
        }

        // If no pattern matches, try to extract the exception type
        var exceptionMatch = Regex.Match(errorMessage, @"(\w+Exception)");
        if (exceptionMatch.Success)
        {
            return exceptionMatch.Groups[1].Value;
        }

        return "Other";
    }
}

public class FlakyAnalyzerOptions
{
    public int TopFlakyTestsToReport { get; set; } = 10;
    public bool AnalyzeErrorPatterns { get; set; } = true;
    public double FlakyThreshold { get; set; } = 0.1; // Consider test flaky if it fails at least 10% of the time
}
