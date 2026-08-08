using BPTracker.Domain.Readings;
using BPTracker.Domain.Trends;
using BPTracker.Presentation.Trends;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Trends;

public sealed class TrendChartSampleBuilderTests
{
    [Fact]
    public void BuildPreservesExactValuesTimeAndTag()
    {
        var measuredAt = TestClock.DefaultNow.AddHours(-3);
        var reading = ReadingFactory.Create(151, 96, measuredAt: measuredAt)
            .WithContext(new MeasurementContext { Tag = "Increased dose" }, TestClock.DefaultNow);
        var smoothed = new TrendPoint(
            DateOnly.FromDateTime(measuredAt.LocalDateTime), 135, 88, 1);

        var sample = TrendChartSampleBuilder.Build([reading], [smoothed]).Single();

        sample.MeasuredAt.ShouldBe(measuredAt);
        sample.Systolic.ShouldBe(151);
        sample.Diastolic.ShouldBe(96);
        sample.SmoothedSystolic.ShouldBe(135);
        sample.Tag.ShouldBe("Increased dose");
        sample.TimeText.ShouldNotBeNullOrWhiteSpace();
        sample.SystolicText.ShouldBe("151 mmHg");
        sample.DiastolicText.ShouldBe("96 mmHg");
    }

    [Fact]
    public void BuildFallsBackToExactValueWhenSmoothingIsUnavailable()
    {
        var sample = TrendChartSampleBuilder.Build([ReadingFactory.Create(127)], []).Single();

        sample.SmoothedSystolic.ShouldBe(127);
    }

    [Fact]
    public void BuildRejectsNullArguments()
    {
        Should.Throw<ArgumentNullException>(() => TrendChartSampleBuilder.Build(null!, []));
        Should.Throw<ArgumentNullException>(() => TrendChartSampleBuilder.Build([], null!));
    }
}
