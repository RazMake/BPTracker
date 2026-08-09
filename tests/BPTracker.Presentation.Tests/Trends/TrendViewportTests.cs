using BPTracker.Application.Trends;
using BPTracker.Presentation.Trends;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Trends;

public sealed class TrendViewportTests
{
    private static readonly TrendWindow Year =
        new(TestClock.DefaultNow.AddDays(-365), TestClock.DefaultNow);

    private static readonly TrendWindow Fortnight =
        new(TestClock.DefaultNow.AddDays(-14), TestClock.DefaultNow);

    [Fact]
    public void TheViewportKeepsAFixedNumberOfDaysOnScreenHoweverMuchIsLoaded()
    {
        var viewport = TrendViewport.For(Year, 0d);

        viewport.VisibleDays.ShouldBe(TrendViewport.DefaultVisibleDays);
        (viewport.To - viewport.From).TotalDays.ShouldBe(TrendViewport.DefaultVisibleDays, 0.001);
    }

    [Fact]
    public void AWindowWiderThanTheScreenCanBeScrolled()
    {
        var viewport = TrendViewport.For(Year, 0d);

        viewport.CanScroll.ShouldBeTrue();
        viewport.MaxOffsetDays.ShouldBe(335, 0.001);
    }

    [Fact]
    public void AWindowShorterThanTheScreenFillsItAndDoesNotScroll()
    {
        var viewport = TrendViewport.For(Fortnight, 0d);

        viewport.VisibleDays.ShouldBe(14, 0.001);
        viewport.MaxOffsetDays.ShouldBe(0, 0.001);
        viewport.CanScroll.ShouldBeFalse();
    }

    [Fact]
    public void ScrollingMovesTheLeftEdgeForwardsByTheOffset() =>
        TrendViewport.For(Year, 100d).From.ShouldBe(Year.From.AddDays(100));

    [Theory]
    [InlineData(-40d, 0d)]
    [InlineData(1000d, 335d)]
    public void TheOffsetIsClampedToTheWindow(double requested, double expected) =>
        TrendViewport.For(Year, requested).OffsetDays.ShouldBe(expected, 0.001);

    [Fact]
    public void AnEmptyWindowStillHasAScreenToDrawOn()
    {
        var viewport = TrendViewport.For(default, 0d);

        viewport.VisibleDays.ShouldBe(TrendViewport.DefaultVisibleDays);
        viewport.CanScroll.ShouldBeFalse();
    }

    [Theory]
    [InlineData(30d, 3d)]
    [InlineData(7d, 1d)]
    [InlineData(90d, 9d)]
    public void LabelsStayAboutTenAcross(double visibleDays, double expectedStep) =>
        TrendViewport.For(Year, 0d, visibleDays).LabelStepDays.ShouldBe(expectedStep, 0.001);

    [Fact]
    public void AScreenHasToBeWiderThanNothing() =>
        Should.Throw<ArgumentOutOfRangeException>(() => TrendViewport.For(Year, 0d, 0d));
}
