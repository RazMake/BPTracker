using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartFrameBuilderTests
{
    private const double Tolerance = 1e-9;

    private static readonly DateTimeOffset Origin = TestClock.DefaultNow;

    private static readonly ChartSample[] TwoSamples =
    [
        new(Origin, 118, 78),
        new(Origin.AddHours(10), 130, 85),
    ];

    private static ChartRequest Request(
        IReadOnlyList<ChartSample>? samples = null,
        double plotWidth = 300,
        double plotHeight = 100,
        double? cursorX = null) => new()
        {
            Samples = samples ?? TwoSamples,
            PlotWidth = plotWidth,
            PlotHeight = plotHeight,
            PixelsPerHour = 1,
            CursorX = cursorX,
        };

    [Fact]
    public void NoSamplesMeansNothingToDraw() =>
        ChartFrameBuilder.Build(Request(samples: [])).HasData.ShouldBeFalse();

    [Fact]
    public void ChartWithNoRoomYetDrawsNothing()
    {
        ChartFrameBuilder.Build(Request(plotWidth: 0)).HasData.ShouldBeFalse();
        ChartFrameBuilder.Build(Request(plotHeight: 0)).HasData.ShouldBeFalse();
    }

    [Fact]
    public void BothLinesGetAPointPerMeasurement()
    {
        var frame = ChartFrameBuilder.Build(Request());

        frame.HasData.ShouldBeTrue();
        frame.Systolic.Dots.Count.ShouldBe(2);
        frame.Diastolic.Dots.Count.ShouldBe(2);
        frame.GridLines.ShouldNotBeEmpty();
        frame.TimeLabels.ShouldNotBeEmpty();
    }

    [Fact]
    public void OnlyTheNormalCorridorsAreShaded()
    {
        var frame = ChartFrameBuilder.Build(Request());

        frame.Bands.Select(band => band.Label).ToArray()
            .ShouldBe([PressureBands.DiastolicLabel, PressureBands.SystolicLabel]);
    }

    [Fact]
    public void ShadedBandsRunTopDownAndHaveHeight()
    {
        var frame = ChartFrameBuilder.Build(Request());

        frame.Bands.ShouldAllBe(band => band.Height > 0);
        frame.Bands.ShouldAllBe(band => band.Top < band.Bottom);
    }

    [Fact]
    public void TheSystolicNormalCorridorSitsAboveTheDiastolicOne()
    {
        var frame = ChartFrameBuilder.Build(Request());

        var systolic = frame.Bands.Single(band => band.Label == PressureBands.SystolicLabel);
        var diastolic = frame.Bands.Single(band => band.Label == PressureBands.DiastolicLabel);

        systolic.Bottom.ShouldBeLessThanOrEqualTo(diastolic.Top);
    }

    [Fact]
    public void WithoutATouchThereIsNoReadOut() =>
        ChartFrameBuilder.Build(Request()).Cursor.ShouldBeNull();

    [Fact]
    public void TheReadOutSnapsToTheNearestMeasurement()
    {
        var frame = ChartFrameBuilder.Build(Request(cursorX: 7));

        frame.Cursor.ShouldNotBeNull();
        frame.Cursor.X.ShouldBe(10, Tolerance);
        frame.Cursor.Sample.ShouldBe(TwoSamples[1]);
    }

    [Fact]
    public void ATouchLeftOfEverythingReadsTheOldestMeasurement()
    {
        var frame = ChartFrameBuilder.Build(Request(cursorX: -500));

        frame.Cursor.ShouldNotBeNull();
        frame.Cursor.Sample.ShouldBe(TwoSamples[0]);
    }

    [Fact]
    public void TheReadOutPrintsBothPressuresAndTheTime()
    {
        var frame = ChartFrameBuilder.Build(Request(cursorX: 0));

        frame.Cursor.ShouldNotBeNull();
        frame.Cursor.ValueText.ShouldBe("118 / 78 mmHg");
        frame.Cursor.TimeText.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AnUntaggedReadingHasNoTagInTheReadOut()
    {
        var frame = ChartFrameBuilder.Build(Request(cursorX: 0));

        frame.Cursor.ShouldNotBeNull();
        frame.Cursor.Tag.ShouldBeNull();
    }

    [Fact]
    public void ATaggedReadingCarriesItsTagIntoTheReadOut()
    {
        var samples = new ChartSample[]
        {
            new(Origin, 118, 78, "after a run"),
            new(Origin.AddHours(10), 130, 85),
        };

        var frame = ChartFrameBuilder.Build(Request(samples: samples, cursorX: 0));

        frame.Cursor.ShouldNotBeNull();
        frame.Cursor.Tag.ShouldBe("after a run");
    }

    [Fact]
    public void OnlyTaggedReadingsGetTheLargerMarker()
    {
        var samples = new ChartSample[]
        {
            new(Origin, 118, 78, "after a run"),
            new(Origin.AddHours(10), 130, 85),
        };

        var frame = ChartFrameBuilder.Build(Request(samples: samples));

        frame.Systolic.Dots[0].IsTagged.ShouldBeTrue();
        frame.Systolic.Dots[1].IsTagged.ShouldBeFalse();
        frame.Diastolic.Dots[0].IsTagged.ShouldBeTrue();
    }

    [Fact]
    public void ABlankTagDoesNotCountAsTagged()
    {
        var frame = ChartFrameBuilder.Build(
            Request(samples: [new ChartSample(Origin, 118, 78, "   "), new ChartSample(Origin.AddHours(1), 120, 80)]));

        frame.Systolic.Dots[0].IsTagged.ShouldBeFalse();
    }

    [Fact]
    public void TheReadOutMarksBothLines()
    {
        var frame = ChartFrameBuilder.Build(Request(cursorX: 0));

        frame.Cursor.ShouldNotBeNull();
        frame.Cursor.Systolic.Y.ShouldBeLessThan(frame.Cursor.Diastolic.Y);
    }

    [Fact]
    public void NullRequestIsRejected() =>
        Should.Throw<ArgumentNullException>(() => ChartFrameBuilder.Build(null!));
}
