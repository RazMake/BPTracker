using BPTracker.Application.Trends;
using BPTracker.TestSupport;

namespace BPTracker.Application.Tests.Trends;

public sealed class TrendPeriodTests
{
    [Theory]
    [InlineData(TrendPeriod.Week, 7)]
    [InlineData(TrendPeriod.Month, 30)]
    [InlineData(TrendPeriod.Quarter, 90)]
    [InlineData(TrendPeriod.Year, 365)]
    public void EachPeriodCoversAFixedNumberOfDays(TrendPeriod period, int expectedDays) =>
        period.Days().ShouldBe(expectedDays);

    [Fact]
    public void NoPeriodReachesBeyondAYear() =>
        Enum.GetValues<TrendPeriod>().Max(period => period.Days()).ShouldBe(365);

    [Fact]
    public void PageZeroIsTheWindowEndingNow()
    {
        var window = TrendPeriod.Month.Page(TestClock.DefaultNow);

        window.To.ShouldBe(TestClock.DefaultNow);
        window.From.ShouldBe(TestClock.DefaultNow.AddDays(-30));
    }

    [Fact]
    public void EachFurtherPageStepsOneWholeWindowBack()
    {
        var window = TrendPeriod.Week.Page(TestClock.DefaultNow, 2);

        window.To.ShouldBe(TestClock.DefaultNow.AddDays(-14));
        window.From.ShouldBe(TestClock.DefaultNow.AddDays(-21));
        window.Length.ShouldBe(TimeSpan.FromDays(7));
    }

    [Fact]
    public void PagingNeverRunsForwards() =>
        Should.Throw<ArgumentOutOfRangeException>(
            () => TrendPeriod.Week.Page(TestClock.DefaultNow, -1));

    [Fact]
    public void AnUnknownPeriodIsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ((TrendPeriod)999).Days());

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
