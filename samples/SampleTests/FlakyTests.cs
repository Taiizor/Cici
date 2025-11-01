using Xunit;

namespace SampleTests;

/// <summary>
/// These tests are intentionally flaky for demonstration purposes
/// They will randomly pass or fail to simulate real-world flaky tests
/// </summary>
public class FlakyTests
{
    private static readonly Random _random = new Random();
    
    [Fact]
    public void RandomFailure_30PercentChance()
    {
        // This test has a 30% chance to fail
        var shouldPass = _random.Next(100) >= 30;
        Assert.True(shouldPass, "Random failure occurred");
    }
    
    [Fact]
    public void RandomFailure_50PercentChance()
    {
        // This test has a 50% chance to fail (most flaky)
        var shouldPass = _random.Next(2) == 0;
        Assert.True(shouldPass, "Random 50/50 failure");
    }
    
    [Fact]
    public async Task TimingDependentTest()
    {
        // This test depends on timing and might fail under load
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(_random.Next(80, 120)); // Random delay between 80-120ms
        stopwatch.Stop();
        
        // Sometimes fails if the delay takes too long
        Assert.True(stopwatch.ElapsedMilliseconds < 110, 
            $"Operation took too long: {stopwatch.ElapsedMilliseconds}ms");
    }
    
    [Fact]
    public void ConcurrencyIssueSimulation()
    {
        // Simulates a race condition
        var counter = 0;
        var tasks = new List<Task>();
        
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                Thread.Sleep(_random.Next(1, 5));
                counter++; // Not thread-safe!
            }));
        }
        
        Task.WaitAll(tasks.ToArray());
        
        // Due to race conditions, this might occasionally fail
        Assert.Equal(10, counter);
    }
    
    [Fact]
    public void MemoryDependentTest()
    {
        // This test might fail if memory pressure is high
        var list = new List<byte[]>();
        var shouldFail = _random.Next(100) < 20; // 20% chance to "simulate" memory pressure
        
        if (shouldFail)
        {
            // Simulate memory issue
            Assert.True(false, "Simulated memory allocation failure");
        }
        
        // Otherwise pass
        for (int i = 0; i < 100; i++)
        {
            list.Add(new byte[1024]); // Allocate 1KB
        }
        
        Assert.True(list.Count == 100);
    }
    
    [Fact]
    public void NetworkSimulationTest()
    {
        // Simulates network issues
        var latency = _random.Next(50, 500); // Random latency
        Thread.Sleep(latency);
        
        // Fail if "network" is too slow
        Assert.True(latency < 300, $"Network timeout: {latency}ms response time");
    }
    
    [Fact]
    public void FileSystemRaceCondition()
    {
        // Simulates file system race conditions
        var fileName = $"test_{Guid.NewGuid()}.tmp";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        
        try
        {
            File.WriteAllText(filePath, "test data");
            
            // Random chance of "file being locked"
            if (_random.Next(100) < 15) // 15% chance
            {
                throw new IOException("Simulated: The process cannot access the file because it is being used by another process");
            }
            
            var content = File.ReadAllText(filePath);
            Assert.Equal("test data", content);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ParameterizedFlakyTest(int value)
    {
        // Different parameters have different flakiness
        var flakyThreshold = value switch
        {
            1 => 10,  // 10% chance to fail
            2 => 30,  // 30% chance to fail
            3 => 50,  // 50% chance to fail
            _ => 0
        };
        
        var shouldPass = _random.Next(100) >= flakyThreshold;
        Assert.True(shouldPass, $"Flaky test failed for value {value}");
    }
}
