using Cici.Models;

namespace Cici.Analyzer
{
    public interface IFlakyAnalyzer
    {
        FlakyTestResult AnalyzeTestResults(TestInfo test, List<TestExecutionResult> executionResults);
        IEnumerable<FlakyTestResult> AnalyzeBatch(Dictionary<TestInfo, List<TestExecutionResult>> testResults);
        FlakyDetectionSummary GenerateSummary(IEnumerable<FlakyTestResult> results);
    }

    public class FlakyDetectionSummary
    {
        public int TotalTests { get; set; }
        public int StableTests { get; set; }
        public int FlakyTests { get; set; }
        public int AlwaysFailingTests { get; set; }
        public double OverallFlakyRate => TotalTests > 0 ? (double)FlakyTests / TotalTests : 0;
        public List<FlakyTestResult> MostFlakyTests { get; set; } = [];
        public Dictionary<string, int> ErrorPatterns { get; set; } = [];
        public TimeSpan TotalExecutionTime { get; set; }
    }
}
