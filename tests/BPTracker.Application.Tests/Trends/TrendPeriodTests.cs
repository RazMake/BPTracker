using BPTracker.Application.Trends;
using BPTracker.TestSupport;

namespace BPTracker.Application.Tests.Trends;

public sealed class TrendPeriodTests
{
    [Theory]
    [InlineData(TrendPeriod.Week, -7)]
    [InlineData(TrendPeriod.Month, -30)]
    [InlineData(TrendPeriod.Quarter, -90)]
    [InlineData(TrendPeriod.Year, -365)]
    public void StartOfSubtractsTheExpectedNumberOfDays(TrendPeriod period, int expectedDayOffset) =>
        period.StartOf(TestClock.DefaultNow).ShouldBe(TestClock.DefaultNow.AddDays(expectedDayOffset));

    [Fact]
    public void StartOfAllReachesBackToTheBeginning() =>
        TrendPeriod.All.StartOf(TestClock.DefaultNow).ShouldBe(DateTimeOffset.MinValue);

    [Fact]
    public void StartOfRejectsUnknownPeriod() =>
        Should.Throw<ArgumentOutOfRangeException>(
            () => ((TrendPeriod)999).StartOf(TestClock.DefaultNow));

    [Fact]
    public void EmptySummaryReportsNoData()
    {
        TrendSummary.Empty.HasData.ShouldBeFalse();
        TrendSummary.Empty.ReadingCount.ShouldBe(0);
    }

    [Fact]
    public void PopulatedSummaryReportsData() =>
        new TrendSummary(120, 80, 130, 110, 4).HasData.ShouldBeTrue();
}
