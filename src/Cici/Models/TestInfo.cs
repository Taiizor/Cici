namespace Cici.Models
{
    /// <summary>
    /// Represents information about a discovered test.
    /// </summary>
    public class TestInfo
    {
        /// <summary>
        /// Gets or sets the fully qualified name of the test.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the class name containing the test.
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the test method name.
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the assembly containing the test.
        /// </summary>
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the test framework used for this test.
        /// </summary>
        public TestFramework Framework { get; set; }

        /// <summary>
        /// Gets or sets additional metadata associated with the test.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = [];
    }

    /// <summary>
    /// Specifies the test framework type.
    /// </summary>
    public enum TestFramework
    {
        /// <summary>
        /// Framework could not be determined.
        /// </summary>
        Unknown,

        /// <summary>
        /// xUnit test framework.
        /// </summary>
        XUnit,

        /// <summary>
        /// NUnit test framework.
        /// </summary>
        NUnit,

        /// <summary>
        /// MSTest framework.
        /// </summary>
        MSTest
    }

    /// <summary>
    /// Represents the result of a single test execution.
    /// </summary>
    public class TestExecutionResult
    {
        /// <summary>
        /// Gets or sets the test that was executed.
        /// </summary>
        public TestInfo Test { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the test passed.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Gets or sets the error message if the test failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the stack trace if the test failed.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the duration of the test execution.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the test was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets the attempt number for this execution.
        /// </summary>
        public int Attempt { get; set; }
    }

    /// <summary>
    /// Represents the aggregated results of multiple test runs for flakiness detection.
    /// </summary>
    public class FlakyTestResult
    {
        /// <summary>
        /// Gets or sets the test that was analyzed.
        /// </summary>
        public TestInfo Test { get; set; } = null!;

        /// <summary>
        /// Gets or sets the detailed execution results for all runs.
        /// </summary>
        public List<TestExecutionResult> ExecutionResults { get; set; } = [];

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
        /// Gets the pass rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double PassRate => TotalRuns > 0 ? (double)PassedCount / TotalRuns : 0;

        /// <summary>
        /// Gets or sets a value indicating whether the test is flaky.
        /// </summary>
        public bool IsFlaky => PassedCount > 0 && FailedCount > 0;

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
    }
}