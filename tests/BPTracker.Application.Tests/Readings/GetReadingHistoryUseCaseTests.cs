using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Application.Tests.Readings;

public sealed class GetReadingHistoryUseCaseTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private GetReadingHistoryUseCase CreateUseCase() => new(_repository, _clock);

    [Fact]
    public async Task ExecuteQueriesTheTrailingWindow()
    {
        await CreateUseCase().ExecuteAsync(30);

        await _repository.Received(1).GetRangeAsync(
            TestClock.DefaultNow.AddDays(-30),
            TestClock.DefaultNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteReturnsWhatTheRepositoryProvides()
    {
        var expected = ReadingFactory.CreateDailySeries(3);
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(expected);

        var readings = await CreateUseCase().ExecuteAsync(7);

        readings.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteRejectsNonPositiveWindow(int days) =>
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateUseCase().ExecuteAsync(days));

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new GetReadingHistoryUseCase(null!, _clock));
        Should.Throw<ArgumentNullException>(() => new GetReadingHistoryUseCase(_repository, null!));
    }
}
