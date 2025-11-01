using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Cici.Models;

namespace Cici.Runner;

public class TestExecutor : ITestExecutor
{
    private readonly TestExecutorOptions _options;

    public TestExecutor(TestExecutorOptions? options = null)
    {
        _options = options ?? new TestExecutorOptions();
    }

    public async Task<TestExecutionResult> ExecuteTestAsync(TestInfo test, int attemptNumber)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestExecutionResult
        {
            Test = test,
            Attempt = attemptNumber,
            ExecutedAt = DateTime.Now
        };

        try
        {
            // Build the dotnet test command
            var arguments = BuildTestArguments(test);
            var workingDir = FindProjectDirectory(test.AssemblyPath);
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            using var process = new Process { StartInfo = processStartInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) => 
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await WaitForProcessAsync(process, TimeSpan.FromSeconds(_options.TimeoutSeconds));

            if (!completed)
            {
                process.Kill(true);
                result.Passed = false;
                result.ErrorMessage = $"Test execution timeout after {_options.TimeoutSeconds} seconds";
            }
            else
            {
                result.Passed = process.ExitCode == 0;
                
                if (!result.Passed && _options.CollectDetailedErrors)
                {
                    var output = outputBuilder.ToString();
                    var error = errorBuilder.ToString();
                    
                    result.ErrorMessage = ExtractErrorMessage(output, error);
                    result.StackTrace = ExtractStackTrace(output);
                }
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"Test execution failed: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<List<TestExecutionResult>> ExecuteTestMultipleTimesAsync(
        TestInfo test, 
        int repeatCount, 
        IProgress<int>? progress = null)
    {
        var results = new List<TestExecutionResult>();

        if (_options.ParallelExecution && repeatCount > 1)
        {
            var semaphore = new SemaphoreSlim(_options.MaxParallelTests);
            var tasks = new List<Task<TestExecutionResult>>();

            for (int i = 1; i <= repeatCount; i++)
            {
                var attemptNumber = i;
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var result = await ExecuteTestAsync(test, attemptNumber);
                        progress?.Report(attemptNumber);
                        return result;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            results.AddRange(await Task.WhenAll(tasks));
        }
        else
        {
            for (int i = 1; i <= repeatCount; i++)
            {
                var result = await ExecuteTestAsync(test, i);
                results.Add(result);
                progress?.Report(i);
                
                // Small delay between sequential runs to avoid resource contention
                if (i < repeatCount)
                    await Task.Delay(100);
            }
        }

        return results;
    }

    private string BuildTestArguments(TestInfo test)
    {
        var filter = BuildTestFilter(test);
        var projectFile = FindProjectFile(test.AssemblyPath);
        
        if (!string.IsNullOrEmpty(projectFile))
        {
            // Use project file if found
            return $"test \"{projectFile}\" --filter \"{filter}\" --logger \"console;verbosity=quiet\" --no-build";
        }
        
        // Fallback to vstest
        return $"vstest \"{test.AssemblyPath}\" --TestCaseFilter:\"{filter}\" --logger:console";
    }

    private string BuildTestFilter(TestInfo test)
    {
        // Build filter based on test framework
        return test.Framework switch
        {
            TestFramework.XUnit => $"FullyQualifiedName={test.FullName}",
            TestFramework.NUnit => $"FullyQualifiedName={test.FullName}",
            TestFramework.MSTest => $"FullyQualifiedName={test.FullName}",
            _ => test.FullName
        };
    }

    private async Task<bool> WaitForProcessAsync(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private string ExtractErrorMessage(string output, string error)
    {
        // Try to extract meaningful error message from test output
        var lines = output.Split('\n').Concat(error.Split('\n'));
        
        foreach (var line in lines)
        {
            if (line.Contains("Assert.") || 
                line.Contains("Expected:") || 
                line.Contains("Actual:") ||
                line.Contains("Error Message:"))
            {
                return line.Trim();
            }
        }

        return !string.IsNullOrEmpty(error) ? error : "Test failed with no specific error message";
    }

    private string? ExtractStackTrace(string output)
    {
        var lines = output.Split('\n');
        var stackTraceLines = new List<string>();
        bool inStackTrace = false;

        foreach (var line in lines)
        {
            if (line.Contains("Stack Trace:") || line.Contains("at "))
            {
                inStackTrace = true;
            }

            if (inStackTrace)
            {
                stackTraceLines.Add(line.Trim());
                if (string.IsNullOrWhiteSpace(line))
                    break;
            }
        }

        return stackTraceLines.Count > 0 ? string.Join("\n", stackTraceLines) : null;
    }
    
    private string FindProjectDirectory(string assemblyPath)
    {
        var dir = Path.GetDirectoryName(assemblyPath);
        
        // Walk up the directory tree to find the project file
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.GetFiles(dir, "*.csproj").Any() ||
                Directory.GetFiles(dir, "*.fsproj").Any() ||
                Directory.GetFiles(dir, "*.vbproj").Any())
            {
                return dir;
            }
            
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        
        // Fallback to assembly directory
        return Path.GetDirectoryName(assemblyPath) ?? Directory.GetCurrentDirectory();
    }
    
    private string? FindProjectFile(string assemblyPath)
    {
        var dir = FindProjectDirectory(assemblyPath);
        
        // Look for project file in the directory
        var projectFiles = Directory.GetFiles(dir, "*.csproj")
            .Concat(Directory.GetFiles(dir, "*.fsproj"))
            .Concat(Directory.GetFiles(dir, "*.vbproj"))
            .ToArray();
        
        if (projectFiles.Length == 1)
        {
            return Path.GetFileName(projectFiles[0]);
        }
        
        // If multiple project files, try to match by assembly name
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var matchingProject = projectFiles.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p).Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        
        return matchingProject != null ? Path.GetFileName(matchingProject) : null;
    }
}
