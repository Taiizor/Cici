namespace Cici.Models
{
    public class TestInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string AssemblyPath { get; set; } = string.Empty;
        public TestFramework Framework { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = [];
    }

    public enum TestFramework
    {
        Unknown,
        XUnit,
        NUnit,
        MSTest
    }

    public class TestExecutionResult
    {
        public TestInfo Test { get; set; } = null!;
        public bool Passed { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime ExecutedAt { get; set; }
        public int Attempt { get; set; }
    }

    public class FlakyTestResult
    {
        public TestInfo Test { get; set; } = null!;
        public List<TestExecutionResult> ExecutionResults { get; set; } = [];
        public int TotalRuns { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public double PassRate => TotalRuns > 0 ? (double)PassedCount / TotalRuns : 0;
        public bool IsFlaky => PassedCount > 0 && FailedCount > 0;
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
    }
}