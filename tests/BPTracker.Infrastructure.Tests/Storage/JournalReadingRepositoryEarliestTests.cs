using BPTracker.TestSupport;

namespace BPTracker.Infrastructure.Tests.Storage;

public sealed class JournalReadingRepositoryEarliestTests
{
    [Fact]
    public async Task FindsTheOldestLiveReading()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        foreach (var reading in ReadingFactory.CreateDailySeries(4))
        {
            await repository.UpsertAsync(reading);
        }

        (await repository.GetEarliestMeasuredAtAsync()).ShouldBe(TestClock.DefaultNow.AddDays(-3));
    }

    [Fact]
    public async Task IgnoresRetractedReadings()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        await repository.UpsertAsync(ReadingFactory.Create(120, 80, TestClock.DefaultNow));

        var old = ReadingFactory.Create(130, 85, TestClock.DefaultNow.AddDays(-10));
        await repository.UpsertAsync(old);
        await repository.UpsertAsync(old.Retract(TestClock.DefaultNow.AddMinutes(1)));

        (await repository.GetEarliestMeasuredAtAsync()).ShouldBe(TestClock.DefaultNow);
    }

    [Fact]
    public async Task IsNullWhenNothingIsStored()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        (await repository.GetEarliestMeasuredAtAsync()).ShouldBeNull();
    }
}
