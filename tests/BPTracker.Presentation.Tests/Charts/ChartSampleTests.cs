using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartSampleTests
{
    [Fact]
    public void AReadingBecomesTheThreeThingsAChartNeeds()
    {
        var reading = ReadingFactory.Create(142, 91, TestClock.DefaultNow);

        var sample = ChartSample.From(reading);

        sample.MeasuredAt.ShouldBe(TestClock.DefaultNow);
        sample.Systolic.ShouldBe(142);
        sample.Diastolic.ShouldBe(91);
    }

    [Fact]
    public void NullReadingIsRejected() =>
        Should.Throw<ArgumentNullException>(() => ChartSample.From(null!));
}
