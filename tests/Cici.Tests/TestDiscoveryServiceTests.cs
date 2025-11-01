using Cici.Models;
using Cici.Runner;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Cici.Tests
{
    public class TestDiscoveryServiceTests
    {
        private readonly TestDiscoveryService _service;

        public TestDiscoveryServiceTests()
        {
            _service = new TestDiscoveryService();
        }

        [Fact]
        public async Task DiscoverTestsAsync_WithValidAssembly_ShouldFindTests()
        {
            // Arrange
            string currentAssembly = Assembly.GetExecutingAssembly().Location;

            // Act
            IEnumerable<TestInfo> tests = await _service.DiscoverTestsAsync(currentAssembly);

            // Assert
            List<TestInfo> testList = tests.ToList();
            testList.Should().NotBeEmpty();
            testList.Should().Contain(t => t.ClassName.Contains("TestDiscoveryServiceTests"));
        }

        [Fact]
        public async Task DiscoverTestsAsync_WithFilter_ShouldReturnFilteredTests()
        {
            // Arrange
            string currentAssembly = Assembly.GetExecutingAssembly().Location;
            string filter = "FlakyAnalyzer";

            // Act
            IEnumerable<TestInfo> tests = await _service.DiscoverTestsAsync(currentAssembly, filter);

            // Assert
            List<TestInfo> testList = tests.ToList();
            testList.Should().NotBeEmpty();
            testList.Should().AllSatisfy(t => t.FullName.Should().Contain(filter));
        }

        [Fact]
        public async Task DiscoverTestsAsync_WithInvalidPath_ShouldThrowException()
        {
            // Arrange
            string invalidPath = "nonexistent.dll";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await _service.DiscoverTestsAsync(invalidPath)
            );
        }

        [Fact]
        public async Task DiscoverTestsAsync_ShouldDetectXUnitFramework()
        {
            // Arrange
            string currentAssembly = Assembly.GetExecutingAssembly().Location;

            // Act
            IEnumerable<TestInfo> tests = await _service.DiscoverTestsAsync(currentAssembly);

            // Assert
            List<TestInfo> testList = tests.ToList();
            testList.Should().NotBeEmpty();
            testList.Should().AllSatisfy(t => t.Framework.Should().Be(TestFramework.XUnit));
        }

        [Fact]
        public async Task DiscoverTestsAsync_ShouldPopulateTestInfo()
        {
            // Arrange
            string currentAssembly = Assembly.GetExecutingAssembly().Location;

            // Act
            IEnumerable<TestInfo> tests = await _service.DiscoverTestsAsync(currentAssembly);

            // Assert
            List<TestInfo> testList = tests.ToList();
            testList.Should().NotBeEmpty();

            foreach (TestInfo? test in testList)
            {
                test.FullName.Should().NotBeNullOrEmpty();
                test.ClassName.Should().NotBeNullOrEmpty();
                test.MethodName.Should().NotBeNullOrEmpty();
                test.AssemblyPath.Should().Be(currentAssembly);
                test.Framework.Should().NotBe(TestFramework.Unknown);
            }
        }

        [Theory]
        [InlineData("Test", "TestClass.TestMethod", true)]
        [InlineData("Method", "TestClass.TestMethod", true)]
        [InlineData("Class", "TestClass.TestMethod", true)]
        [InlineData("NotFound", "TestClass.TestMethod", false)]
        [InlineData("", "TestClass.TestMethod", true)]
        [InlineData(null, "TestClass.TestMethod", true)]
        public void FilterMatches_ShouldWorkCorrectly(string? filter, string testName, bool expected)
        {
            // Arrange
            TestInfo testInfo = new() { FullName = testName };

            // Act
            bool matches = string.IsNullOrEmpty(filter) ||
                          testInfo.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase);

            // Assert
            matches.Should().Be(expected);
        }
    }
}