using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartScrollTests
{
    private const double Tolerance = 1e-9;

    private static readonly ChartSample[] TwoDaysApart =
    [
        new(TestClock.DefaultNow.AddDays(-2), 120, 80),
        new(TestClock.DefaultNow, 130, 85),
    ];

    [Fact]
    public void WithNoSamplesTheContentIsExactlyTheVisibleWidth() =>
        ChartScroll.ContentWidth([], 2.5, 300).ShouldBe(300, Tolerance);

    [Fact]
    public void ContentWidthFollowsTheElapsedTime() =>
        ChartScroll.ContentWidth(TwoDaysApart, 10, 300).ShouldBe(480, Tolerance);

    [Fact]
    public void AShortHistoryCannotBeScrolledAway() =>
        ChartScroll.ContentWidth(TwoDaysApart, 1, 300).ShouldBe(300, Tolerance);

    [Fact]
    public void AZeroZoomIsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ChartScroll.ContentWidth(TwoDaysApart, 0, 300));

    [Fact]
    public void NullSamplesAreRejected() =>
        Should.Throw<ArgumentNullException>(() => ChartScroll.ContentWidth(null!, 1, 300));

    [Theory]
    [InlineData(-50, 0)]
    [InlineData(90, 90)]
    [InlineData(500, 180)]
    public void TheOffsetStaysInsideTheScrollableRange(double offset, double expected) =>
        ChartScroll.Clamp(offset, 480, 300).ShouldBe(expected, Tolerance);

    [Fact]
    public void ContentNarrowerThanTheScreenCannotScroll() =>
        ChartScroll.Clamp(120, 200, 300).ShouldBe(0, Tolerance);
}
