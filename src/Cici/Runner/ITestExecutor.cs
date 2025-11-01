using Cici.Models;

namespace Cici.Runner
{
    /// <summary>
    /// Defines the contract for test execution services.
    /// </summary>
    public interface ITestExecutor
    {
        /// <summary>
        /// Executes a single test asynchronously.
        /// </summary>
        /// <param name="test">The test to execute.</param>
        /// <param name="attemptNumber">The attempt number for this execution.</param>
        /// <returns>A task containing the execution result.</returns>
        Task<TestExecutionResult> ExecuteTestAsync(TestInfo test, int attemptNumber);

        /// <summary>
        /// Executes a test multiple times for flakiness detection.
        /// </summary>
        /// <param name="test">The test to execute.</param>
        /// <param name="repeatCount">The number of times to execute the test.</param>
        /// <param name="progress">Optional progress reporter for tracking execution.</param>
        /// <returns>A task containing all execution results.</returns>
        Task<List<TestExecutionResult>> ExecuteTestMultipleTimesAsync(TestInfo test, int repeatCount, IProgress<int>? progress = null);
    }

    /// <summary>
    /// Configuration options for test execution.
    /// </summary>
    public class TestExecutorOptions
    {
        /// <summary>
        /// Gets or sets the timeout in seconds for each test execution. Default is 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to run test iterations in parallel.
        /// </summary>
        public bool ParallelExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of tests to run in parallel. Default is the processor count.
        /// </summary>
        public int MaxParallelTests { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets a value indicating whether to collect detailed error information.
        /// </summary>
        public bool CollectDetailedErrors { get; set; } = true;
    }
}