using BPTracker.Domain.Readings;
using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartAxisBuilderTests
{
    private const double Tolerance = 1e-9;

    private static readonly DateTimeOffset Origin = TestClock.DefaultNow;

    private static readonly int[] ExpectedGridValues = [40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140];

    private static readonly int[] ExpectedHealthyEdges =
    [
        HealthyRange.Diastolic.Lowest,
        HealthyRange.Diastolic.TooHigh,
        HealthyRange.Systolic.Lowest,
        HealthyRange.Systolic.TooHigh,
    ];

    private static ChartScale Scale(double pixelsPerHour = 4) =>
        new(Origin, pixelsPerHour, 0, 100, 40, 140);

    [Fact]
    public void GridLinesCoverTheWholeAxisInRoundSteps()
    {
        var values = ChartAxisBuilder.GridLines(Scale()).Select(line => line.Value).ToArray();

        values.ShouldBe(ExpectedGridValues);
    }

    [Fact]
    public void TheHealthyBandEdgesAreMarkedSoTheyCanBeDrawnDifferently()
    {
        var lines = ChartAxisBuilder.GridLines(Scale());

        lines.Where(line => line.IsHealthyEdge).Select(line => line.Value).ToArray()
            .ShouldBe(ExpectedHealthyEdges);
    }

    [Fact]
    public void AGridLineSitsWhereTheScalePutsItsValue()
    {
        var scale = Scale();

        var line = ChartAxisBuilder.GridLines(scale).Single(candidate => candidate.Value == 90);

        line.Y.ShouldBe(scale.Y(90), Tolerance);
    }

    [Fact]
    public void TimeLabelsFillTheVisibleWindowAndStopAtItsEdge()
    {
        var labels = ChartAxisBuilder.TimeLabels(Scale(), plotWidth: 300);

        labels.Count.ShouldBe(3);
        labels.ShouldAllBe(label => label.X >= 0 && label.X <= 300);
        labels[0].Text.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1)]
    [InlineData(0.2)]
    public void TicksNeverCrowdTogetherHoweverFarTheChartIsZoomedOut(double pixelsPerHour)
    {
        var labels = ChartAxisBuilder.TimeLabels(Scale(pixelsPerHour), plotWidth: 300);

        labels.ShouldNotBeEmpty();
        for (var index = 1; index < labels.Count; index++)
        {
            (labels[index].X - labels[index - 1].X).ShouldBeGreaterThan(70);
        }
    }

    [Fact]
    public void AVeryLowZoomLabelsMonthsRatherThanDays()
    {
        var labels = ChartAxisBuilder.TimeLabels(Scale(pixelsPerHour: 0.1), plotWidth: 300);

        labels.ShouldNotBeEmpty();
        labels[0].Text.ShouldContain("2026");
    }

    [Fact]
    public void ANarrowWindowProducesNoTicks() =>
        ChartAxisBuilder.TimeLabels(Scale(), plotWidth: 0).ShouldBeEmpty();

    [Fact]
    public void NullScaleIsRejected()
    {
        Should.Throw<ArgumentNullException>(() => ChartAxisBuilder.GridLines(null!));
        Should.Throw<ArgumentNullException>(() => ChartAxisBuilder.TimeLabels(null!, 300));
    }
}
