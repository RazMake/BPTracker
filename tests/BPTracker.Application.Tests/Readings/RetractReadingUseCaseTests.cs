using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Application.Tests.Readings;

public sealed class RetractReadingUseCaseTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private RetractReadingUseCase CreateUseCase() => new(_repository, _clock);

    [Fact]
    public async Task ExecuteRetractsAnExistingReading()
    {
        var existing = ReadingFactory.Create();
        _repository.FindAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateUseCase().ExecuteAsync(existing.Id);

        result.ShouldBeTrue();
        await _repository.Received(1).UpsertAsync(
            Arg.Is<BloodPressureReading>(reading => reading!.IsDeleted && reading.Id == existing.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteStampsTheRetractionTime()
    {
        var existing = ReadingFactory.Create();
        _repository.FindAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _clock.Advance(TimeSpan.FromHours(2));

        await CreateUseCase().ExecuteAsync(existing.Id);

        await _repository.Received(1).UpsertAsync(
            Arg.Is<BloodPressureReading>(reading => reading!.UpdatedAtUtc == _clock.UtcNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteReportsFailureForUnknownReading()
    {
        _repository.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((BloodPressureReading?)null);

        var result = await CreateUseCase().ExecuteAsync(Guid.CreateVersion7());

        result.ShouldBeFalse();
        await _repository.DidNotReceive().UpsertAsync(
            Arg.Any<BloodPressureReading>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new RetractReadingUseCase(null!, _clock));
        Should.Throw<ArgumentNullException>(() => new RetractReadingUseCase(_repository, null!));
    }
}
