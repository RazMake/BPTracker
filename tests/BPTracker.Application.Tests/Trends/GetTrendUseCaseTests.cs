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

    [Fact]
    public async Task ExecuteQueriesTheRequestedPeriod()
    {
        await CreateUseCase().ExecuteAsync(TrendPeriod.Month);

        await _repository.Received(1).GetRangeAsync(
            TestClock.DefaultNow.AddDays(-30),
            TestClock.DefaultNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteReturnsOnePointPerDay()
    {
        GivenReadings(5);

        var result = await CreateUseCase().ExecuteAsync(TrendPeriod.Week);

        result.Daily.Count.ShouldBe(5);
        result.Smoothed.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ExecuteSummarisesTheWindow()
    {
        GivenReadings(3);

        var summary = (await CreateUseCase().ExecuteAsync(TrendPeriod.Week)).Summary;

        summary.HasData.ShouldBeTrue();
        summary.ReadingCount.ShouldBe(3);
        summary.LowestSystolic.ShouldBe(120);
        summary.HighestSystolic.ShouldBe(122);
        summary.AverageDiastolic.ShouldBe(80, 0.001);
    }

    [Fact]
    public async Task ExecuteReturnsEmptySummaryWhenThereAreNoReadings()
    {
        var result = await CreateUseCase().ExecuteAsync(TrendPeriod.Year);

        result.Summary.HasData.ShouldBeFalse();
        result.Summary.ShouldBe(TrendSummary.Empty);
        result.Daily.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ExecuteRejectsNonPositiveSmoothingWindow(int window) =>
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => CreateUseCase().ExecuteAsync(TrendPeriod.Week, window));

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new GetTrendUseCase(null!, _clock));
        Should.Throw<ArgumentNullException>(() => new GetTrendUseCase(_repository, null!));
    }
}
