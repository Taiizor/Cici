using Cici.Analyzer;
using Cici.Models;
using Cici.Reporter;
using Cici.Runner;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Cici.Tests
{
    public class CiciRunnerTests
    {
        private readonly ITestDiscoveryService _mockDiscoveryService;
        private readonly ITestExecutor _mockExecutor;
        private readonly IFlakyAnalyzer _mockAnalyzer;
        private readonly IReporter _mockReporter;
        private readonly CiciRunner _runner;

        public CiciRunnerTests()
        {
            _mockDiscoveryService = Substitute.For<ITestDiscoveryService>();
            _mockExecutor = Substitute.For<ITestExecutor>();
            _mockAnalyzer = Substitute.For<IFlakyAnalyzer>();
            _mockReporter = Substitute.For<IReporter>();

            _runner = new CiciRunner(
                _mockDiscoveryService,
                _mockExecutor,
                _mockAnalyzer,
                [_mockReporter]
            );
        }

        [Fact]
        public async Task RunAsync_WithNoTests_ShouldReturnEmptyResult()
        {
            // Arrange
            CiciRunOptions options = new()
            {
                AssemblyPath = "test.dll",
                RepeatCount = 5
            };

            _mockDiscoveryService
                .DiscoverTestsAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(Task.FromResult<IEnumerable<TestInfo>>([]));

            // Act
            CiciRunResult result = await _runner.RunAsync(options);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.TotalTests.Should().Be(0);
            result.FlakyTests.Should().Be(0);
            result.StableTests.Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_WithSuccessfulTests_ShouldReturnCorrectCounts()
        {
            // Arrange
            CiciRunOptions options = new()
            {
                AssemblyPath = "test.dll",
                RepeatCount = 3
            };

            List<TestInfo> tests =
            [
                new() { FullName = "Test1" },
                new() { FullName = "Test2" }
            ];

            _mockDiscoveryService
                .DiscoverTestsAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(Task.FromResult<IEnumerable<TestInfo>>(tests));

            _mockExecutor
                .ExecuteTestMultipleTimesAsync(Arg.Any<TestInfo>(), Arg.Any<int>(), Arg.Any<IProgress<int>?>())
                .Returns(Task.FromResult(new List<TestExecutionResult>
                {
                    new() { Passed = true },
                    new() { Passed = true },
                    new() { Passed = true }
                }));

            List<FlakyTestResult> flakyResults =
            [
                new() { PassedCount = 3, FailedCount = 0, TotalRuns = 3 },
                new() { PassedCount = 2, FailedCount = 1, TotalRuns = 3 }
            ];

            _mockAnalyzer
                .AnalyzeBatch(Arg.Any<Dictionary<TestInfo, List<TestExecutionResult>>>())
                .Returns(flakyResults);

            _mockAnalyzer
                .GenerateSummary(Arg.Any<List<FlakyTestResult>>())
                .Returns(new FlakyDetectionSummary
                {
                    TotalTests = 2,
                    FlakyTests = 1,
                    StableTests = 1,
                    AlwaysFailingTests = 0
                });

            // Act
            CiciRunResult result = await _runner.RunAsync(options);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.TotalTests.Should().Be(2);
            result.FlakyTests.Should().Be(1);
            result.StableTests.Should().Be(1);
            result.AlwaysFailingTests.Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_WithProgress_ShouldReportProgress()
        {
            // Arrange
            CiciRunOptions options = new()
            {
                AssemblyPath = "test.dll",
                RepeatCount = 2
            };

            List<TestInfo> tests = [new() { FullName = "Test1" }];
            List<double> progressReports = [];
            Progress<double> progress = new(progressReports.Add);

            _mockDiscoveryService
                .DiscoverTestsAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(Task.FromResult<IEnumerable<TestInfo>>(tests));

            _mockExecutor
                .ExecuteTestMultipleTimesAsync(Arg.Any<TestInfo>(), Arg.Any<int>(), Arg.Any<IProgress<int>?>())
                .Returns(async callInfo =>
                {
                    IProgress<int>? progress = callInfo.ArgAt<IProgress<int>?>(2);
                    progress?.Report(1);
                    await Task.Delay(10);
                    progress?.Report(2);
                    return
                    [
                        new() { Passed = true },
                        new() { Passed = true }
                    ];
                });

            List<FlakyTestResult> flakyResults = [new() { PassedCount = 2, FailedCount = 0, TotalRuns = 2 }];
            _mockAnalyzer
                .AnalyzeBatch(Arg.Any<Dictionary<TestInfo, List<TestExecutionResult>>>())
                .Returns(flakyResults);

            _mockAnalyzer
                .GenerateSummary(Arg.Any<List<FlakyTestResult>>())
                .Returns(new FlakyDetectionSummary());

            // Act
            await _runner.RunAsync(options, progress);

            // Assert
            progressReports.Should().NotBeEmpty();
            progressReports.Should().Contain(p => p > 0 && p <= 100);
        }

        [Fact]
        public async Task RunAsync_WithException_ShouldReturnFailedResult()
        {
            // Arrange
            CiciRunOptions options = new()
            {
                AssemblyPath = "test.dll",
                RepeatCount = 5
            };

            string expectedError = "Test discovery failed";
            _mockDiscoveryService
                .DiscoverTestsAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns<IEnumerable<TestInfo>>(x => throw new InvalidOperationException(expectedError));

            // Act
            CiciRunResult result = await _runner.RunAsync(options);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be(expectedError);
        }

        [Fact]
        public async Task RunAsync_ShouldCallReporters()
        {
            // Arrange
            CiciRunOptions options = new()
            {
                AssemblyPath = "test.dll",
                RepeatCount = 1
            };

            List<TestInfo> tests = [new() { FullName = "Test1" }];

            _mockDiscoveryService
                .DiscoverTestsAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(Task.FromResult<IEnumerable<TestInfo>>(tests));

            _mockExecutor
                .ExecuteTestMultipleTimesAsync(Arg.Any<TestInfo>(), Arg.Any<int>(), Arg.Any<IProgress<int>?>())
                .Returns(Task.FromResult(new List<TestExecutionResult> { new() { Passed = true } }));

            List<FlakyTestResult> flakyResults = [new() { PassedCount = 1, FailedCount = 0, TotalRuns = 1 }];
            _mockAnalyzer
                .AnalyzeBatch(Arg.Any<Dictionary<TestInfo, List<TestExecutionResult>>>())
                .Returns(flakyResults);

            FlakyDetectionSummary summary = new();
            _mockAnalyzer
                .GenerateSummary(Arg.Any<List<FlakyTestResult>>())
                .Returns(summary);

            // Act
            await _runner.RunAsync(options);

            // Assert
            await _mockReporter.Received(1).GenerateReportAsync(
                Arg.Is<IEnumerable<FlakyTestResult>>(r => r.Count() == 1),
                Arg.Is<FlakyDetectionSummary>(s => s == summary)
            );
        }

        [Fact]
        public void Constructor_WithNullServices_ShouldUseDefaults()
        {
            // Act
            CiciRunner runner = new();

            // Assert
            runner.Should().NotBeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RunAsync_ShouldRespectParallelExecutionOption(bool parallel)
        {
            // Arrange
            CiciRunOptions options = new()
            {
                AssemblyPath = "test.dll",
                RepeatCount = 3,
                ParallelExecution = parallel
            };

            List<TestInfo> tests = [new() { FullName = "Test1" }];

            _mockDiscoveryService
                .DiscoverTestsAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(Task.FromResult<IEnumerable<TestInfo>>(tests));

            _mockExecutor
                .ExecuteTestMultipleTimesAsync(Arg.Any<TestInfo>(), Arg.Any<int>(), Arg.Any<IProgress<int>?>())
                .Returns(Task.FromResult(new List<TestExecutionResult> { new() { Passed = true } }));

            _mockAnalyzer
                .AnalyzeBatch(Arg.Any<Dictionary<TestInfo, List<TestExecutionResult>>>())
                .Returns([new()]);

            _mockAnalyzer
                .GenerateSummary(Arg.Any<List<FlakyTestResult>>())
                .Returns(new FlakyDetectionSummary());

            // Act
            await _runner.RunAsync(options);

            // Assert
            await _mockExecutor.Received(1).ExecuteTestMultipleTimesAsync(
                Arg.Any<TestInfo>(),
                options.RepeatCount,
                Arg.Any<IProgress<int>?>()
            );
        }
    }
}