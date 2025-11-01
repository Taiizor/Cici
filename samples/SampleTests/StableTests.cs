using Xunit;

namespace SampleTests
{
    /// <summary>
    /// These tests should always pass consistently
    /// </summary>
    public class StableTests
    {
        [Fact]
        public void MathAddition_ShouldAlwaysPass()
        {
            int result = 2 + 2;
            Assert.Equal(4, result);
        }

        [Fact]
        public void StringConcatenation_ShouldAlwaysPass()
        {
            string result = "Hello" + " " + "World";
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void ListOperations_ShouldAlwaysPass()
        {
            List<int> list = [1, 2, 3, 4];
            Assert.Equal(4, list.Count);
            Assert.Contains(4, list);
        }

        [Fact]
        public async Task AsyncOperation_ShouldAlwaysPass()
        {
            await Task.Delay(10); // Short, consistent delay
            int result = await Task.FromResult(42);
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2, 3, 5)]
        [InlineData(10, 15, 25)]
        public void ParameterizedTest_ShouldAlwaysPass(int a, int b, int expected)
        {
            int result = a + b;
            Assert.Equal(expected, result);
        }
    }
}
