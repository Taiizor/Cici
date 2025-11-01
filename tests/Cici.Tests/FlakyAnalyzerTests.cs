using Cici.Analyzer;
using Cici.Models;
using FluentAssertions;
using Xunit;

namespace Cici.Tests
{
    public class FlakyAnalyzerTests
    {
        private readonly FlakyAnalyzer _analyzer;

        public FlakyAnalyzerTests()
        {
            _analyzer = new FlakyAnalyzer();
        }

        [Fact]
        public void Analyze_WithAllPassingTests_ShouldReturnStable()
        {
            // Arrange
            TestInfo test = new() { FullName = "TestClass.TestMethod" };
            List<TestExecutionResult> results =
            [
                new() { Test = test, Passed = true, Attempt = 1 },
                new() { Test = test, Passed = true, Attempt = 2 },
                new() { Test = test, Passed = true, Attempt = 3 }
            ];

            // Act
            FlakyTestResult result = _analyzer.AnalyzeTestResults(test, results);

            // Assert
            result.Should().NotBeNull();
            result.IsFlaky.Should().BeFalse();
            result.PassRate.Should().Be(1.0);
            result.TotalRuns.Should().Be(3);
            result.PassedCount.Should().Be(3);
            result.FailedCount.Should().Be(0);
        }

        [Fact]
        public void Analyze_WithAllFailingTests_ShouldReturnAlwaysFailing()
        {
            // Arrange
            TestInfo test = new() { FullName = "TestClass.TestMethod" };
            List<TestExecutionResult> results =
            [
                new() { Test = test, Passed = false, Attempt = 1, ErrorMessage = "Test failed" },
                new() { Test = test, Passed = false, Attempt = 2, ErrorMessage = "Test failed" },
                new() { Test = test, Passed = false, Attempt = 3, ErrorMessage = "Test failed" }
            ];

            // Act
            FlakyTestResult result = _analyzer.AnalyzeTestResults(test, results);

            // Assert
            result.Should().NotBeNull();
            result.IsFlaky.Should().BeFalse();
            result.PassRate.Should().Be(0.0);
            result.TotalRuns.Should().Be(3);
            result.PassedCount.Should().Be(0);
            result.FailedCount.Should().Be(3);
        }

        [Fact]
        public void Analyze_WithMixedResults_ShouldReturnFlaky()
        {
            // Arrange
            TestInfo test = new() { FullName = "TestClass.TestMethod" };
            List<TestExecutionResult> results =
            [
                new() { Test = test, Passed = true, Attempt = 1 },
                new() { Test = test, Passed = false, Attempt = 2, ErrorMessage = "Random failure" },
                new() { Test = test, Passed = true, Attempt = 3 },
                new() { Test = test, Passed = false, Attempt = 4, ErrorMessage = "Another failure" }
            ];

            // Act
            FlakyTestResult result = _analyzer.AnalyzeTestResults(test, results);

            // Assert
            result.Should().NotBeNull();
            result.IsFlaky.Should().BeTrue();
            result.PassRate.Should().Be(0.5);
            result.TotalRuns.Should().Be(4);
            result.PassedCount.Should().Be(2);
            result.FailedCount.Should().Be(2);
        }

        [Fact]
        public void AnalyzeBatch_WithMultipleTests_ShouldReturnCorrectResults()
        {
            // Arrange
            TestInfo test1 = new() { FullName = "Test1" };
            TestInfo test2 = new() { FullName = "Test2" };

            Dictionary<TestInfo, List<TestExecutionResult>> testResults = new()
            {
                {
                    test1,
                    new List<TestExecutionResult>
                    {
                        new() { Test = test1, Passed = true, Attempt = 1 },
                        new() { Test = test1, Passed = true, Attempt = 2 }
                    }
                },
                {
                    test2,
                    new List<TestExecutionResult>
                    {
                        new() { Test = test2, Passed = true, Attempt = 1 },
                        new() { Test = test2, Passed = false, Attempt = 2, ErrorMessage = "Flaky" }
                    }
                }
            };

            // Act
            List<FlakyTestResult> results = _analyzer.AnalyzeBatch(testResults).ToList();

            // Assert
            results.Should().HaveCount(2);
            results[0].IsFlaky.Should().BeFalse();
            results[1].IsFlaky.Should().BeTrue();
        }

        [Fact]
        public void GenerateSummary_ShouldCalculateStatisticsCorrectly()
        {
            // Arrange
            List<FlakyTestResult> results =
            [
                new() { IsFlaky = true, PassedCount = 3, FailedCount = 2, TotalRuns = 5 },
                new() { IsFlaky = true, PassedCount = 1, FailedCount = 4, TotalRuns = 5 },
                new() { IsFlaky = false, PassedCount = 5, FailedCount = 0, TotalRuns = 5 },
                new() { IsFlaky = false, PassedCount = 0, FailedCount = 5, TotalRuns = 5 }
            ];

            // Act
            FlakyDetectionSummary summary = _analyzer.GenerateSummary(results);

            // Assert
            summary.Should().NotBeNull();
            summary.TotalTests.Should().Be(4);
            summary.FlakyTests.Should().Be(2);
            summary.StableTests.Should().Be(1);
            summary.AlwaysFailingTests.Should().Be(1);
            summary.TotalExecutions.Should().Be(20);
        }

        [Theory]
        [InlineData(0, 10, 0.0)]
        [InlineData(5, 5, 0.5)]
        [InlineData(10, 0, 1.0)]
        [InlineData(0, 0, 0.0)]
        public void PassRate_ShouldBeCalculatedCorrectly(int passed, int failed, double expectedRate)
        {
            // Arrange
            TestInfo test = new() { FullName = "Test" };
            List<TestExecutionResult> results = [];

            for (int i = 0; i < passed; i++)
            {
                results.Add(new() { Test = test, Passed = true, Attempt = i + 1 });
            }

            for (int i = 0; i < failed; i++)
            {
                results.Add(new() { Test = test, Passed = false, Attempt = passed + i + 1 });
            }

            // Act
            FlakyTestResult result = _analyzer.AnalyzeTestResults(test, results);

            // Assert
            result.PassRate.Should().BeApproximately(expectedRate, 0.001);
        }

        [Fact]
        public void Analyze_ShouldCalculateDurationStatistics()
        {
            // Arrange
            TestInfo test = new() { FullName = "TestClass.TestMethod" };
            List<TestExecutionResult> results =
            [
                new() { Test = test, Passed = true, Attempt = 1, Duration = TimeSpan.FromMilliseconds(100) },
                new() { Test = test, Passed = true, Attempt = 2, Duration = TimeSpan.FromMilliseconds(200) },
                new() { Test = test, Passed = true, Attempt = 3, Duration = TimeSpan.FromMilliseconds(300) }
            ];

            // Act
            FlakyTestResult result = _analyzer.AnalyzeTestResults(test, results);

            // Assert
            result.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(200));
            result.MinDuration.Should().Be(TimeSpan.FromMilliseconds(100));
            result.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(300));
        }
    }
}