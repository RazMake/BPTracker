using BPTracker.Application.Abstractions;
using BPTracker.Application.Trends;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Application.Tests.Trends;

public sealed class GetTrendUseCaseTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private GetTrendUseCase CreateUseCase() => new(_repository, _clock);

    private void GivenReadings(int days) =>
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(ReadingFactory.CreateDailySeries(days));

    private static TrendRequest Request(
        TrendPeriod period = TrendPeriod.Week,
        int pageIndex = 0,
        int smoothingWindowDays = TrendRequest.DefaultSmoothingWindowDays) =>
        new() { Period = period, PageIndex = pageIndex, SmoothingWindowDays = smoothingWindowDays };

    [Fact]
    public async Task ExecuteQueriesTheRequestedPeriod()
    {
        await CreateUseCase().ExecuteAsync(Request(TrendPeriod.Month));

        await _repository.Received(1).GetRangeAsync(
            TestClock.DefaultNow.AddDays(-30),
            TestClock.DefaultNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteQueriesOneWholeWindowBackPerPage()
    {
        var result = await CreateUseCase().ExecuteAsync(Request(TrendPeriod.Month, pageIndex: 2));

        await _repository.Received(1).GetRangeAsync(
            TestClock.DefaultNow.AddDays(-90),
            TestClock.DefaultNow.AddDays(-60),
            Arg.Any<CancellationToken>());
        result.Window.From.ShouldBe(TestClock.DefaultNow.AddDays(-90));
        result.Window.To.ShouldBe(TestClock.DefaultNow.AddDays(-60));
    }

    [Fact]
    public async Task ExecuteReportsTheOldestReadingOnRecordSoTheScreenCanPage()
    {
        var earliest = TestClock.DefaultNow.AddYears(-3);
        _repository.GetEarliestMeasuredAtAsync(Arg.Any<CancellationToken>()).Returns(earliest);

        var result = await CreateUseCase().ExecuteAsync(Request());

        result.EarliestMeasuredAt.ShouldBe(earliest);
    }

    [Fact]
    public async Task ExecuteReturnsOnePointPerDay()
    {
        GivenReadings(5);

        var result = await CreateUseCase().ExecuteAsync(Request());

        result.Readings.Count.ShouldBe(5);
        result.Daily.Count.ShouldBe(5);
        result.Smoothed.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ExecutePreservesExactReadingTimesOldestFirst()
    {
        GivenReadings(3);

        var result = await CreateUseCase().ExecuteAsync(Request());

        result.Readings.Select(reading => reading.MeasuredAt).ToArray().ShouldBe(
        [
            TestClock.DefaultNow.AddDays(-2),
            TestClock.DefaultNow.AddDays(-1),
            TestClock.DefaultNow,
        ]);
    }

    [Fact]
    public async Task ExecuteSummarisesTheWindow()
    {
        GivenReadings(3);

        var summary = (await CreateUseCase().ExecuteAsync(Request())).Summary;

        summary.HasData.ShouldBeTrue();
        summary.ReadingCount.ShouldBe(3);
        summary.LowestSystolic.ShouldBe(120);
        summary.HighestSystolic.ShouldBe(122);
        summary.AverageDiastolic.ShouldBe(80, 0.001);
    }

    [Fact]
    public async Task ExecuteReturnsEmptySummaryWhenThereAreNoReadings()
    {
        var result = await CreateUseCase().ExecuteAsync(Request(TrendPeriod.Year));

        result.Summary.HasData.ShouldBeFalse();
        result.Summary.ShouldBe(TrendSummary.Empty);
        result.Readings.ShouldBeEmpty();
        result.Daily.ShouldBeEmpty();
        result.EarliestMeasuredAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ExecuteRejectsNonPositiveSmoothingWindow(int window) =>
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => CreateUseCase().ExecuteAsync(Request(smoothingWindowDays: window)));

    [Fact]
    public async Task ExecuteRejectsAMissingRequest() =>
        await Should.ThrowAsync<ArgumentNullException>(() => CreateUseCase().ExecuteAsync(null!));

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new GetTrendUseCase(null!, _clock));
        Should.Throw<ArgumentNullException>(() => new GetTrendUseCase(_repository, null!));
    }
}
