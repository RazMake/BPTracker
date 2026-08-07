using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Application.Tests.Readings;

public sealed class AddReadingUseCaseTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private AddReadingUseCase CreateUseCase() => new(_repository, _clock);

    [Fact]
    public async Task ExecutePersistsTheReading()
    {
        var reading = await CreateUseCase().ExecuteAsync(new AddReadingRequest(128, 82));

        await _repository.Received(1).UpsertAsync(reading, Arg.Any<CancellationToken>());
        reading.Systolic.MmHg.ShouldBe(128);
        reading.Diastolic.MmHg.ShouldBe(82);
    }

    [Fact]
    public async Task ExecuteStampsUpdatedTimeFromTheClock()
    {
        var reading = await CreateUseCase().ExecuteAsync(new AddReadingRequest(120, 80));

        reading.UpdatedAtUtc.ShouldBe(TestClock.DefaultNow);
    }

    [Fact]
    public async Task ExecuteDefaultsMeasuredAtToNow()
    {
        var reading = await CreateUseCase().ExecuteAsync(new AddReadingRequest(120, 80));

        reading.MeasuredAt.ShouldBe(TestClock.DefaultNow);
    }

    [Fact]
    public async Task ExecuteHonoursAnExplicitMeasurementTime()
    {
        var measuredAt = TestClock.DefaultNow.AddHours(-3);

        var reading = await CreateUseCase().ExecuteAsync(new AddReadingRequest(120, 80, measuredAt));

        reading.MeasuredAt.ShouldBe(measuredAt);
    }

    [Fact]
    public async Task ExecuteCarriesTheMeasurementContext()
    {
        var request = new AddReadingRequest(
            120,
            80,
            Context: new MeasurementContext { Arm = MeasurementArm.Left, Note = " rest " });

        var reading = await CreateUseCase().ExecuteAsync(request);

        reading.Context.Arm.ShouldBe(MeasurementArm.Left);
        reading.Context.Note.ShouldBe("rest");
    }

    [Fact]
    public async Task ExecuteRejectsImplausiblePressures()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => CreateUseCase().ExecuteAsync(new AddReadingRequest(400, 80)));

        await _repository.DidNotReceive().UpsertAsync(
            Arg.Any<BloodPressureReading>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteRejectsSystolicBelowDiastolic() =>
        await Should.ThrowAsync<ArgumentException>(
            () => CreateUseCase().ExecuteAsync(new AddReadingRequest(80, 120)));

    [Fact]
    public async Task ExecuteRejectsNullRequest() =>
        await Should.ThrowAsync<ArgumentNullException>(() => CreateUseCase().ExecuteAsync(null!));

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new AddReadingUseCase(null!, _clock));
        Should.Throw<ArgumentNullException>(() => new AddReadingUseCase(_repository, null!));
    }
}
