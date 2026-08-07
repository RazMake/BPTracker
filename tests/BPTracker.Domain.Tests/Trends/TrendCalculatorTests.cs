using BPTracker.Domain.Trends;
using BPTracker.TestSupport;

namespace BPTracker.Domain.Tests.Trends;

public sealed class TrendCalculatorTests
{
    [Fact]
    public void DailyAveragesGroupsReadingsTakenOnTheSameDay()
    {
        var day = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var readings = new[]
        {
            ReadingFactory.Create(120, 80, day.AddHours(8)),
            ReadingFactory.Create(140, 90, day.AddHours(20)),
        };

        var points = TrendCalculator.DailyAverages(readings);

        points.Count.ShouldBe(1);
        points[0].AverageSystolic.ShouldBe(130, 0.001);
        points[0].AverageDiastolic.ShouldBe(85, 0.001);
        points[0].ReadingCount.ShouldBe(2);
    }

    [Fact]
    public void DailyAveragesOrdersOldestFirst()
    {
        var points = TrendCalculator.DailyAverages(ReadingFactory.CreateDailySeries(3));

        points.Count.ShouldBe(3);
        points.ShouldBeInOrder(SortDirection.Ascending, Comparer<TrendPoint>.Create(
            (left, right) => left.Day.CompareTo(right.Day)));
    }

    [Fact]
    public void DailyAveragesIgnoresRetractedReadings()
    {
        var day = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var readings = new[]
        {
            ReadingFactory.Create(120, 80, day),
            ReadingFactory.Create(200, 110, day).Retract(TestClock.DefaultNow),
        };

        var points = TrendCalculator.DailyAverages(readings);

        points.Count.ShouldBe(1);
        points[0].AverageSystolic.ShouldBe(120, 0.001);
        points[0].ReadingCount.ShouldBe(1);
    }

    [Fact]
    public void DailyAveragesReturnsEmptyForNoReadings() =>
        TrendCalculator.DailyAverages([]).ShouldBeEmpty();

    [Fact]
    public void DailyAveragesRejectsNull() =>
        Should.Throw<ArgumentNullException>(() => TrendCalculator.DailyAverages(null!));

    [Fact]
    public void MovingAverageSmoothsAcrossTheWindow()
    {
        var points = new List<TrendPoint>
        {
            new(new DateOnly(2026, 4, 1), 100, 60, 1),
            new(new DateOnly(2026, 4, 2), 110, 70, 1),
            new(new DateOnly(2026, 4, 3), 120, 80, 1),
        };

        var smoothed = TrendCalculator.MovingAverage(points, 3);

        // Leading points average over fewer samples so the series stays aligned on the x-axis.
        smoothed[0].AverageSystolic.ShouldBe(100, 0.001);
        smoothed[1].AverageSystolic.ShouldBe(105, 0.001);
        smoothed[2].AverageSystolic.ShouldBe(110, 0.001);
        smoothed[2].AverageDiastolic.ShouldBe(70, 0.001);
    }

    [Fact]
    public void MovingAveragePreservesDaysAndPointCount()
    {
        var points = TrendCalculator.DailyAverages(ReadingFactory.CreateDailySeries(5));
        var smoothed = TrendCalculator.MovingAverage(points, 2);

        smoothed.Count.ShouldBe(points.Count);
        smoothed.Select(point => point.Day).ShouldBe(points.Select(point => point.Day));
    }

    [Fact]
    public void MovingAverageWithWindowOfOneReturnsTheInput()
    {
        var points = TrendCalculator.DailyAverages(ReadingFactory.CreateDailySeries(4));
        var smoothed = TrendCalculator.MovingAverage(points, 1);

        smoothed.Select(point => point.AverageSystolic)
            .ShouldBe(points.Select(point => point.AverageSystolic));
    }

    [Fact]
    public void MovingAverageSumsReadingCountsInsideTheWindow()
    {
        var points = new List<TrendPoint>
        {
            new(new DateOnly(2026, 4, 1), 100, 60, 2),
            new(new DateOnly(2026, 4, 2), 110, 70, 3),
        };

        TrendCalculator.MovingAverage(points, 2)[1].ReadingCount.ShouldBe(5);
    }

    [Fact]
    public void MovingAverageReturnsEmptyForEmptyInput() =>
        TrendCalculator.MovingAverage([], 7).ShouldBeEmpty();

    [Fact]
    public void MovingAverageRejectsNull() =>
        Should.Throw<ArgumentNullException>(() => TrendCalculator.MovingAverage(null!, 7));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MovingAverageRejectsNonPositiveWindow(int window) =>
        Should.Throw<ArgumentOutOfRangeException>(() => TrendCalculator.MovingAverage([], window));
}
