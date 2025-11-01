using Cici.Models;
using System.Diagnostics;
using System.Text;

namespace Cici.Runner
{
    public class TestExecutor : ITestExecutor
    {
        private readonly TestExecutorOptions _options;

        public TestExecutor(TestExecutorOptions? options = null)
        {
            _options = options ?? new TestExecutorOptions();
        }

        public async Task<TestExecutionResult> ExecuteTestAsync(TestInfo test, int attemptNumber)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            TestExecutionResult result = new()
            {
                Test = test,
                Attempt = attemptNumber,
                ExecutedAt = DateTime.Now
            };

            try
            {
                // Build the dotnet test command
                (string? arguments, string? workingDirectory) = BuildTestCommand(test);

                ProcessStartInfo processStartInfo = new()
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                using Process process = new() { StartInfo = processStartInfo };
                StringBuilder outputBuilder = new();
                StringBuilder errorBuilder = new();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool completed = await WaitForProcessAsync(process, TimeSpan.FromSeconds(_options.TimeoutSeconds));

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
                        string output = outputBuilder.ToString();
                        string error = errorBuilder.ToString();

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
            List<TestExecutionResult> results = [];

            if (_options.ParallelExecution && repeatCount > 1)
            {
                SemaphoreSlim semaphore = new(_options.MaxParallelTests);
                List<Task<TestExecutionResult>> tasks = [];

                for (int i = 1; i <= repeatCount; i++)
                {
                    int attemptNumber = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            TestExecutionResult result = await ExecuteTestAsync(test, attemptNumber);
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
                    TestExecutionResult result = await ExecuteTestAsync(test, i);
                    results.Add(result);
                    progress?.Report(i);

                    // Small delay between sequential runs to avoid resource contention
                    if (i < repeatCount)
                    {
                        await Task.Delay(100);
                    }
                }
            }

            return results;
        }

        private (string arguments, string workingDirectory) BuildTestCommand(TestInfo test)
        {
            string filter = BuildTestFilter(test);
            string? projectFile = FindProjectFile(test.AssemblyPath);

            if (!string.IsNullOrEmpty(projectFile))
            {
                // Use project file if found
                string projectDir = Path.GetDirectoryName(projectFile) ?? Directory.GetCurrentDirectory();
                string projectName = Path.GetFileName(projectFile);

                // Use --configuration Release/Debug based on the assembly path
                string config = test.AssemblyPath.Contains("\\Release\\") || test.AssemblyPath.Contains("/Release/")
                    ? "Release" : "Debug";

                // Use relative project file name since we set working directory
                string arguments = $"test \"{projectName}\" --filter \"{filter}\" --logger \"console;verbosity=quiet\" --configuration {config} --no-build --no-restore";

                return (arguments, projectDir);
            }

            // If no project file found, try vstest as fallback
            string assemblyDir = Path.GetDirectoryName(test.AssemblyPath) ?? Directory.GetCurrentDirectory();

            // Last fallback: try vstest (note: this may not work in all scenarios)
            string vstestArgs = $"vstest \"{test.AssemblyPath}\" /TestCaseFilter:\"{filter}\" /logger:console";
            return (vstestArgs, assemblyDir);
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
            using CancellationTokenSource cts = new(timeout);

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
            IEnumerable<string> lines = output.Split('\n').Concat(error.Split('\n'));

            foreach (string? line in lines)
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
            string[] lines = output.Split('\n');
            List<string> stackTraceLines = [];
            bool inStackTrace = false;

            foreach (string line in lines)
            {
                if (line.Contains("Stack Trace:") || line.Contains("at "))
                {
                    inStackTrace = true;
                }

                if (inStackTrace)
                {
                    stackTraceLines.Add(line.Trim());
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        break;
                    }
                }
            }

            return stackTraceLines.Count > 0 ? string.Join("\n", stackTraceLines) : null;
        }

        private string FindProjectDirectory(string assemblyPath)
        {
            string? dir = Path.GetDirectoryName(assemblyPath);

            // Walk up the directory tree to find the project file
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.GetFiles(dir, "*.csproj").Any() ||
                    Directory.GetFiles(dir, "*.fsproj").Any() ||
                    Directory.GetFiles(dir, "*.vbproj").Any())
                {
                    return dir;
                }

                DirectoryInfo? parent = Directory.GetParent(dir);
                if (parent == null)
                {
                    break;
                }

                dir = parent.FullName;
            }

            // Fallback to assembly directory
            return Path.GetDirectoryName(assemblyPath) ?? Directory.GetCurrentDirectory();
        }

        private string? FindProjectFile(string assemblyPath)
        {
            string dir = FindProjectDirectory(assemblyPath);

            // Look for project file in the directory
            string[] projectFiles = Directory.GetFiles(dir, "*.csproj")
                .Concat(Directory.GetFiles(dir, "*.fsproj"))
                .Concat(Directory.GetFiles(dir, "*.vbproj"))
                .ToArray();

            if (projectFiles.Length == 1)
            {
                return projectFiles[0]; // Return full path
            }

            // If multiple project files, try to match by assembly name
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            string? matchingProject = projectFiles.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p).Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

            return matchingProject; // Return full path
        }
    }
}