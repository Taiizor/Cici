using Xunit;

namespace SampleTests;

/// <summary>
/// These tests always fail - representing broken tests that need fixing
/// </summary>
public class AlwaysFailingTests
{
    [Fact]
    public void BrokenAssertion_AlwaysFails()
    {
        Assert.Equal(5, 2 + 2); // This will always fail
    }
    
    [Fact]
    public void NullReferenceTest_AlwaysFails()
    {
        string? nullString = null;
        Assert.NotNull(nullString); // This will always fail
        var length = nullString!.Length; // Would throw if assertion didn't fail first
    }
    
    [Fact]
    public void ExceptionTest_AlwaysFails()
    {
        throw new InvalidOperationException("This test always throws an exception");
    }
    
    [Fact]
    public async Task AsyncTimeout_AlwaysFails()
    {
        // Simulates a test that always times out
        await Task.Delay(60000); // 60 second delay (will timeout)
        Assert.Fail("This should never be reached");
    }
    
    [Theory]
    [InlineData(1, 2, 0)] // Wrong expected value
    [InlineData(2, 3, 0)] // Wrong expected value
    [InlineData(5, 5, 0)] // Wrong expected value
    public void WrongExpectations_AlwaysFails(int a, int b, int expected)
    {
        var actual = a + b;
        Assert.Equal(expected, actual); // Will always fail due to wrong expected values
    }
}
