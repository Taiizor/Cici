using Cici.Models;

namespace Cici.Analyzer
{
    /// <summary>
    /// Defines the contract for analyzing test execution results to detect flaky tests.
    /// </summary>
    public interface IFlakyAnalyzer
    {
        /// <summary>
        /// Analyzes execution results for a single test to determine flakiness.
        /// </summary>
        /// <param name="test">The test information.</param>
        /// <param name="executionResults">List of execution results from multiple runs.</param>
        /// <returns>Analysis result containing flakiness metrics.</returns>
        FlakyTestResult AnalyzeTestResults(TestInfo test, List<TestExecutionResult> executionResults);
        
        /// <summary>
        /// Analyzes execution results for multiple tests in batch.
        /// </summary>
        /// <param name="testResults">Dictionary mapping tests to their execution results.</param>
        /// <returns>Collection of analysis results for all tests.</returns>
        IEnumerable<FlakyTestResult> AnalyzeBatch(Dictionary<TestInfo, List<TestExecutionResult>> testResults);
        
        /// <summary>
        /// Generates a summary report from analyzed test results.
        /// </summary>
        /// <param name="results">Collection of analyzed test results.</param>
        /// <returns>Summary containing aggregate statistics and patterns.</returns>
        FlakyDetectionSummary GenerateSummary(IEnumerable<FlakyTestResult> results);
    }

    /// <summary>
    /// Contains summary statistics and insights from flaky test detection analysis.
    /// </summary>
    public class FlakyDetectionSummary
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
        /// Gets or sets the number of flaky tests detected (inconsistent results).
        /// </summary>
        public int FlakyTests { get; set; }
        
        /// <summary>
        /// Gets or sets the number of tests that failed in all executions.
        /// </summary>
        public int AlwaysFailingTests { get; set; }
        
        /// <summary>
        /// Gets the overall flaky rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double OverallFlakyRate => TotalTests > 0 ? (double)FlakyTests / TotalTests : 0;
        
        /// <summary>
        /// Gets or sets the list of tests with highest flakiness scores.
        /// </summary>
        public List<FlakyTestResult> MostFlakyTests { get; set; } = new List<FlakyTestResult>();
        
        /// <summary>
        /// Gets or sets the error patterns found across all tests with occurrence counts.
        /// </summary>
        public Dictionary<string, int> ErrorPatterns { get; set; } = new Dictionary<string, int>();
        
        /// <summary>
        /// Gets or sets the total time spent executing all tests.
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }
    }
}