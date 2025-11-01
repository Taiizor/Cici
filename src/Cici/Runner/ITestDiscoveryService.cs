using Cici.Models;

namespace Cici.Runner
{
    /// <summary>
    /// Defines the contract for test discovery services that can identify tests in assemblies.
    /// </summary>
    public interface ITestDiscoveryService
    {
        /// <summary>
        /// Discovers all tests in the specified assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the test assembly.</param>
        /// <returns>A task containing the collection of discovered tests.</returns>
        Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath);

        /// <summary>
        /// Discovers tests in the specified assembly with optional filtering.
        /// </summary>
        /// <param name="assemblyPath">The path to the test assembly.</param>
        /// <param name="filter">Optional filter to apply to test discovery (supports partial name matching).</param>
        /// <returns>A task containing the collection of discovered tests matching the filter.</returns>
        Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath, string? filter);
    }
}