using Cici.Models;

namespace Cici.Runner
{
    public interface ITestExecutor
    {
        Task<TestExecutionResult> ExecuteTestAsync(TestInfo test, int attemptNumber);
        Task<List<TestExecutionResult>> ExecuteTestMultipleTimesAsync(TestInfo test, int repeatCount, IProgress<int>? progress = null);
    }

    public class TestExecutorOptions
    {
        public int TimeoutSeconds { get; set; } = 30;
        public bool ParallelExecution { get; set; } = false;
        public int MaxParallelTests { get; set; } = Environment.ProcessorCount;
        public bool CollectDetailedErrors { get; set; } = true;
    }
}
