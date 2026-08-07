using BPTracker.Presentation.Charts;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartTouchTests
{
    [Theory]
    [InlineData(0, ChartGesture.Scroll)]
    [InlineData(109, ChartGesture.Scroll)]
    [InlineData(110, ChartGesture.Scroll)]
    [InlineData(111, ChartGesture.Inspect)]
    [InlineData(200, ChartGesture.Inspect)]
    public void TheTouchHeightDecidesWhatTheGestureDoes(double y, ChartGesture expected) =>
        ChartTouch.GestureFor(y, 200).ShouldBe(expected);

    [Fact]
    public void BeforeTheChartHasBeenSizedEveryTouchScrolls() =>
        ChartTouch.GestureFor(50, 0).ShouldBe(ChartGesture.Scroll);
}
