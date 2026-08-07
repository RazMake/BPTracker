using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartScaleTests
{
    private const double Tolerance = 1e-9;

    private static readonly DateTimeOffset Origin = TestClock.DefaultNow;

    private static ChartScale Create(double offset = 0) => new(Origin, 2, offset, 100, 40, 140);

    [Fact]
    public void TimeIsMappedProportionally()
    {
        var scale = Create();

        scale.X(Origin).ShouldBe(0, Tolerance);
        scale.X(Origin.AddHours(10)).ShouldBe(20, Tolerance);
        scale.X(Origin.AddHours(30)).ShouldBe(60, Tolerance);
    }

    [Fact]
    public void ScrollingMovesEverythingLeftByTheOffset() =>
        Create(offset: 15).X(Origin.AddHours(10)).ShouldBe(5, Tolerance);

    [Fact]
    public void TheValueAxisGrowsUpwards()
    {
        var scale = Create();

        scale.Y(40).ShouldBe(100, Tolerance);
        scale.Y(90).ShouldBe(50, Tolerance);
        scale.Y(140).ShouldBe(0, Tolerance);
    }

    [Fact]
    public void PointCombinesBothAxes()
    {
        var point = Create().Point(Origin.AddHours(10), 90);

        point.X.ShouldBe(20, Tolerance);
        point.Y.ShouldBe(50, Tolerance);
    }

    [Fact]
    public void TimeAtIsTheInverseOfX() =>
        Create(offset: 15).TimeAt(5).ShouldBe(Origin.AddHours(10));

    [Fact]
    public void AZeroZoomIsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new ChartScale(Origin, 0, 0, 100, 40, 140));

    [Fact]
    public void AZeroHeightIsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new ChartScale(Origin, 2, 0, 0, 40, 140));

    [Fact]
    public void AnInvertedValueAxisIsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new ChartScale(Origin, 2, 0, 100, 140, 40));
}
