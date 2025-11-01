using Cici.Analyzer;
using Cici.Models;
using Cici.Reporter;
using Cici.Runner;

namespace Cici
{
    /// <summary>
    /// The main orchestrator for flaky test detection. Coordinates test discovery, execution, analysis, and reporting.
    /// </summary>
    public class CiciRunner
    {
        private readonly ITestDiscoveryService _discoveryService;
        private readonly ITestExecutor _executor;
        private readonly IFlakyAnalyzer _analyzer;
        private readonly List<IReporter> _reporters;

        /// <summary>
        /// Initializes a new instance of the <see cref="CiciRunner"/> class.
        /// </summary>
        /// <param name="discoveryService">Service for discovering tests in assemblies. If null, uses default implementation.</param>
        /// <param name="executor">Service for executing tests. If null, uses default implementation.</param>
        /// <param name="analyzer">Service for analyzing test results for flakiness. If null, uses default implementation.</param>
        /// <param name="reporters">List of reporters for outputting results. If null, uses console reporter only.</param>
        public CiciRunner(
            ITestDiscoveryService? discoveryService = null,
            ITestExecutor? executor = null,
            IFlakyAnalyzer? analyzer = null,
            List<IReporter>? reporters = null)
        {
            _discoveryService = discoveryService ?? new TestDiscoveryService();
            _executor = executor ?? new TestExecutor();
            _analyzer = analyzer ?? new FlakyAnalyzer();
            _reporters = reporters ?? [new ConsoleReporter()];
        }

        /// <summary>
        /// Runs the flaky test detection process asynchronously.
        /// </summary>
        /// <param name="options">Configuration options for the test run.</param>
        /// <param name="overallProgress">Optional progress reporter for tracking execution progress.</param>
        /// <returns>A task representing the asynchronous operation, containing the run results.</returns>
        public async Task<CiciRunResult> RunAsync(CiciRunOptions options, IProgress<double>? overallProgress = null)
        {
            CiciRunResult runResult = new();

            try
            {
                // Step 1: Discover tests
                Console.WriteLine($"🔍 Discovering tests in {Path.GetFileName(options.AssemblyPath)}...");
                IEnumerable<TestInfo> tests = await _discoveryService.DiscoverTestsAsync(options.AssemblyPath, options.TestFilter);
                List<TestInfo> testList = tests.ToList();

                if (!testList.Any())
                {
                    Console.WriteLine("❌ No tests found matching the criteria.");
                    runResult.Success = true; // No tests is a valid state, not an error
                    return runResult;
                }

                Console.WriteLine($"📋 Found {testList.Count} test(s) to analyze");
                Console.WriteLine();

                // Step 2: Execute tests multiple times
                Dictionary<TestInfo, List<TestExecutionResult>> testResults = [];
                int currentTest = 0;
                int totalTests = testList.Count;

                // Calculate total operations for progress tracking
                int totalOperations = testList.Count * options.RepeatCount;
                int completedOperations = 0;

                foreach (TestInfo test in testList)
                {
                    currentTest++;
                    Console.WriteLine($"[{currentTest}/{totalTests}] Running: {test.MethodName}");

                    Progress<int> progress = new(attempt =>
                    {
                        Console.Write($"\r  Attempt {attempt}/{options.RepeatCount}");

                        // Update overall progress
                        completedOperations++;
                        overallProgress?.Report((double)completedOperations / totalOperations * 100);
                    });

                    List<TestExecutionResult> results = await _executor.ExecuteTestMultipleTimesAsync(test, options.RepeatCount, progress);
                    testResults[test] = results;

                    Console.WriteLine($"\r  ✓ Completed {options.RepeatCount} runs");
                }

                Console.WriteLine();

                // Step 3: Analyze results
                Console.WriteLine("📊 Analyzing test results...");
                List<FlakyTestResult> analyzedResults = _analyzer.AnalyzeBatch(testResults).ToList();
                FlakyDetectionSummary summary = _analyzer.GenerateSummary(analyzedResults);

                // Step 4: Generate reports
                foreach (IReporter reporter in _reporters)
                {
                    await reporter.GenerateReportAsync(analyzedResults, summary);

                    if (reporter is IFileReporter fileReporter)
                    {
                        Console.WriteLine($"📝 Report saved to: {fileReporter.OutputPath}");
                    }
                }

                // Populate run result
                runResult.Success = true;
                runResult.TotalTests = summary.TotalTests;
                runResult.FlakyTests = summary.FlakyTests;
                runResult.StableTests = summary.StableTests;
                runResult.AlwaysFailingTests = summary.AlwaysFailingTests;
                runResult.Results = analyzedResults;
                runResult.Summary = summary;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();

                runResult.Success = false;
                runResult.ErrorMessage = ex.Message;
            }

            return runResult;
        }
    }

    /// <summary>
    /// Configuration options for running flaky test detection.
    /// </summary>
    public class CiciRunOptions
    {
        /// <summary>
        /// Gets or sets the path to the test assembly to analyze.
        /// </summary>
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional filter to apply when discovering tests.
        /// </summary>
        public string? TestFilter { get; set; }

        /// <summary>
        /// Gets or sets the number of times to run each test. Default is 10.
        /// </summary>
        public int RepeatCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether to run test iterations in parallel.
        /// </summary>
        public bool ParallelExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets the types of reports to generate. Default is Console only.
        /// </summary>
        public List<ReportType> ReportTypes { get; set; } = [ReportType.Console];

        /// <summary>
        /// Gets or sets the directory where reports should be saved.
        /// </summary>
        public string? OutputDirectory { get; set; }
    }

    /// <summary>
    /// Specifies the type of report to generate.
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// Console output with colored formatting.
        /// </summary>
        Console,

        /// <summary>
        /// JSON file output for programmatic consumption.
        /// </summary>
        Json,

        /// <summary>
        /// HTML report for viewing in a browser.
        /// </summary>
        Html
    }

    /// <summary>
    /// Contains the results of a flaky test detection run.
    /// </summary>
    public class CiciRunResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the run completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the run failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the total number of tests analyzed.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Gets or sets the number of flaky tests detected.
        /// </summary>
        public int FlakyTests { get; set; }

        /// <summary>
        /// Gets or sets the number of stable tests.
        /// </summary>
        public int StableTests { get; set; }

        /// <summary>
        /// Gets or sets the number of tests that always fail.
        /// </summary>
        public int AlwaysFailingTests { get; set; }

        /// <summary>
        /// Gets or sets the detailed results for each test.
        /// </summary>
        public List<FlakyTestResult> Results { get; set; } = [];

        /// <summary>
        /// Gets or sets the summary of the detection run.
        /// </summary>
        public FlakyDetectionSummary? Summary { get; set; }
    }
}