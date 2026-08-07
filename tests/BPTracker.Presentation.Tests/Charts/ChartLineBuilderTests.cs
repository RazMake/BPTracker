using BPTracker.Domain.Readings;
using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartLineBuilderTests
{
    private const double Tolerance = 1e-9;

    private static readonly DateTimeOffset Origin = TestClock.DefaultNow;
    private static readonly ChartScale Scale = new(Origin, 1, 0, 100, 40, 220);

    private static ChartLine BuildSystolic(params ChartSample[] samples) =>
        ChartLineBuilder.Build(samples, sample => sample.Systolic, CrisisThreshold.Systolic, Scale);

    private static ChartSample[] Pair(int first, int second) =>
    [
        new(Origin, first, 80),
        new(Origin.AddHours(10), second, 80),
    ];

    [Fact]
    public void NoSamplesProduceNothingToDraw() =>
        BuildSystolic().ShouldBe(ChartLine.Empty);

    [Fact]
    public void ASingleSampleIsADotWithNoLine()
    {
        var line = BuildSystolic(new ChartSample(Origin, 118, 78));

        line.Dots.Count.ShouldBe(1);
        line.Segments.ShouldBeEmpty();
    }

    [Fact]
    public void ARunWellBelowTheThresholdIsOneOrdinarySegment()
    {
        var line = BuildSystolic(Pair(110, 130));

        line.Segments.Count.ShouldBe(1);
        line.Segments[0].IsCritical.ShouldBeFalse();
    }

    [Fact]
    public void ARunWhollyAboveTheThresholdIsOneCriticalSegment()
    {
        var line = BuildSystolic(Pair(185, 195));

        line.Segments.Count.ShouldBe(1);
        line.Segments[0].IsCritical.ShouldBeTrue();
    }

    [Fact]
    public void ARunIsSplitExactlyWhereItCrossesTheCrisisThreshold()
    {
        // 171 to 191 crosses 181 half way along.
        var line = BuildSystolic(Pair(171, 191));

        line.Segments.Count.ShouldBe(2);
        line.Segments[0].IsCritical.ShouldBeFalse();
        line.Segments[1].IsCritical.ShouldBeTrue();
        line.Segments[0].To.X.ShouldBe(5, Tolerance);
        line.Segments[1].From.X.ShouldBe(5, Tolerance);
    }

    [Fact]
    public void AFallingRunIsSplitTheSameWay()
    {
        var line = BuildSystolic(Pair(191, 171));

        line.Segments.Count.ShouldBe(2);
        line.Segments[0].IsCritical.ShouldBeTrue();
        line.Segments[1].IsCritical.ShouldBeFalse();
    }

    [Fact]
    public void ARunEndingExactlyOnTheThresholdIsNotSplit()
    {
        var line = BuildSystolic(Pair(171, CrisisThreshold.Systolic));

        line.Segments.Count.ShouldBe(1);
        line.Segments[0].IsCritical.ShouldBeFalse();
    }

    [Fact]
    public void ADotKnowsWhetherItsOwnValueIsACrisis()
    {
        var line = BuildSystolic(Pair(171, 191));

        line.Dots[0].IsCritical.ShouldBeFalse();
        line.Dots[1].IsCritical.ShouldBeTrue();
    }

    [Fact]
    public void TheDiastolicThresholdIsUsedForTheDiastolicLine()
    {
        var samples = new ChartSample[]
        {
            new(Origin, 150, 110),
            new(Origin.AddHours(10), 150, 130),
        };

        var line = ChartLineBuilder.Build(
            samples, sample => sample.Diastolic, CrisisThreshold.Diastolic, Scale);

        line.Segments.Count.ShouldBe(2);
        line.Segments[1].IsCritical.ShouldBeTrue();
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        Should.Throw<ArgumentNullException>(() =>
            ChartLineBuilder.Build(null!, sample => sample.Systolic, 181, Scale));

        Should.Throw<ArgumentNullException>(() =>
            ChartLineBuilder.Build([], null!, 181, Scale));

        Should.Throw<ArgumentNullException>(() =>
            ChartLineBuilder.Build([], sample => sample.Systolic, 181, null!));
    }
}
