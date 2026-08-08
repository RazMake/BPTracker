using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;
using BPTracker.Presentation.Trends;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartValueBoundsTests
{
    private static ChartSample Sample(int systolic, int diastolic) =>
        new(TestClock.DefaultNow, systolic, diastolic);

    [Fact]
    public void WithNoSamplesTheAxisStillCoversBothHealthyBands()
    {
        var bounds = ChartValueBounds.For([]);

        bounds.Lowest.ShouldBe(50);
        bounds.Highest.ShouldBe(130);
    }

    [Fact]
    public void AHighReadingRaisesTheTopOfTheAxis() =>
        ChartValueBounds.For([Sample(185, 95)]).Highest.ShouldBe(190);

    [Fact]
    public void ALowReadingDropsTheBottomOfTheAxis() =>
        ChartValueBounds.For([Sample(120, 45)]).Lowest.ShouldBe(40);

    [Fact]
    public void OrdinaryReadingsDoNotMoveTheAxis()
    {
        var bounds = ChartValueBounds.For([Sample(118, 76), Sample(125, 82)]);

        bounds.Lowest.ShouldBe(50);
        bounds.Highest.ShouldBe(130);
    }

    [Fact]
    public void TrendBoundsFollowTheVisibleDailyAverages()
    {
        var points = new TrendChartSample[]
        {
            new(TestClock.DefaultNow.AddDays(-1), 118, 78, 118, null),
            new(TestClock.DefaultNow, 151, 96, 135, null),
        };

        var bounds = ChartValueBounds.ForTrend(points);

        bounds.Lowest.ShouldBe(50);
        bounds.Highest.ShouldBe(160);
    }

    [Fact]
    public void NullSamplesAreRejected() =>
        Should.Throw<ArgumentNullException>(() => ChartValueBounds.For(null!));

    [Fact]
    public void NullTrendPointsAreRejected() =>
        Should.Throw<ArgumentNullException>(() => ChartValueBounds.ForTrend(null!));
}
