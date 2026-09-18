using NevaxisZentra.Desktop.Services;

namespace NevaxisZentra.Desktop.Tests;

public sealed class NativeWindowBoundsTests
{
    [Fact]
    public void NativeWindowBoundsMatchesExactDimensions()
    {
        var left = new NativeWindowBounds(50, 60, 1600, 1000);
        var right = new NativeWindowBounds(50, 60, 1600, 1000);

        Assert.True(left.Matches(right));
        Assert.Equal(1650, left.Right);
        Assert.Equal(1060, left.Bottom);
    }
}
