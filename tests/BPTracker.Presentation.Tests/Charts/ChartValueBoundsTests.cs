using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartValueBoundsTests
{
    private static ChartSample Sample(int systolic, int diastolic) =>
        new(TestClock.DefaultNow, systolic, diastolic);

    [Fact]
    public void WithNoSamplesTheAxisStillCoversBothHealthyBands()
    {
        var bounds = ChartValueBounds.For([]);

        bounds.Lowest.ShouldBe(40);
        bounds.Highest.ShouldBe(140);
    }

    [Fact]
    public void AHighReadingRaisesTheTopOfTheAxis() =>
        ChartValueBounds.For([Sample(185, 95)]).Highest.ShouldBe(200);

    [Fact]
    public void ALowReadingDropsTheBottomOfTheAxis() =>
        ChartValueBounds.For([Sample(120, 45)]).Lowest.ShouldBe(20);

    [Fact]
    public void OrdinaryReadingsDoNotMoveTheAxis()
    {
        var bounds = ChartValueBounds.For([Sample(118, 76), Sample(125, 82)]);

        bounds.Lowest.ShouldBe(40);
        bounds.Highest.ShouldBe(140);
    }

    [Fact]
    public void NullSamplesAreRejected() =>
        Should.Throw<ArgumentNullException>(() => ChartValueBounds.For(null!));
}
