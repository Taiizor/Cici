using Cici.Models;

namespace Cici.Runner;

public interface ITestDiscoveryService
{
    Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath);
    Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath, string? filter);
}
