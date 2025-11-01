using Cici.Analyzer;
using Cici.Models;
using Cici.Reporter;
using Cici.Runner;

namespace Cici
{
    public class CiciRunner
    {
        private readonly ITestDiscoveryService _discoveryService;
        private readonly ITestExecutor _executor;
        private readonly IFlakyAnalyzer _analyzer;
        private readonly List<IReporter> _reporters;

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

        public async Task<CiciRunResult> RunAsync(CiciRunOptions options)
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
                    return runResult;
                }

                Console.WriteLine($"📋 Found {testList.Count} test(s) to analyze");
                Console.WriteLine();

                // Step 2: Execute tests multiple times
                Dictionary<TestInfo, List<TestExecutionResult>> testResults = [];
                int currentTest = 0;
                int totalTests = testList.Count;

                foreach (TestInfo test in testList)
                {
                    currentTest++;
                    Console.WriteLine($"[{currentTest}/{totalTests}] Running: {test.MethodName}");

                    Progress<int> progress = new(attempt =>
                    {
                        Console.Write($"\r  Attempt {attempt}/{options.RepeatCount}");
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

    public class CiciRunOptions
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string? TestFilter { get; set; }
        public int RepeatCount { get; set; } = 10;
        public bool ParallelExecution { get; set; } = false;
        public List<ReportType> ReportTypes { get; set; } = [ReportType.Console];
        public string? OutputDirectory { get; set; }
    }

    public enum ReportType
    {
        Console,
        Json,
        Html
    }

    public class CiciRunResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalTests { get; set; }
        public int FlakyTests { get; set; }
        public int StableTests { get; set; }
        public int AlwaysFailingTests { get; set; }
        public List<FlakyTestResult> Results { get; set; } = [];
        public FlakyDetectionSummary? Summary { get; set; }
    }
}
