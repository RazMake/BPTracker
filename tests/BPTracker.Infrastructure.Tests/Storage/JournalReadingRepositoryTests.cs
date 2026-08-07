using BPTracker.Domain.Readings;
using BPTracker.Infrastructure.Storage;
using BPTracker.TestSupport;

namespace BPTracker.Infrastructure.Tests.Storage;

public sealed class JournalReadingRepositoryTests
{
    [Fact]
    public async Task UpsertThenFindRoundTrips()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();
        var reading = ReadingFactory.Create(134, 87);

        await repository.UpsertAsync(reading);

        (await repository.FindAsync(reading.Id)).ShouldNotBeNull().Id.ShouldBe(reading.Id);
    }

    [Fact]
    public async Task WritesOnlyToThisDevicesJournal()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        await repository.UpsertAsync(ReadingFactory.Create());

        var files = Directory.GetFiles(fixture.Location.DataFolder, "readings-*.ndjson");
        files.Length.ShouldBe(1);
        files[0].ShouldBe(fixture.Location.DeviceJournalPath);
    }

    [Fact]
    public async Task AppendsRatherThanRewriting()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        await repository.UpsertAsync(ReadingFactory.Create(120, 80));
        await repository.UpsertAsync(ReadingFactory.Create(130, 85));

        (await File.ReadAllLinesAsync(fixture.Location.DeviceJournalPath)).Length.ShouldBe(2);
    }

    [Fact]
    public async Task AnEditAppendsAndTheLatestWins()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var original = ReadingFactory.Create();
        await repository.UpsertAsync(original);

        var edited = original.WithContext(
            new MeasurementContext { Note = "corrected" },
            TestClock.DefaultNow.AddMinutes(10));
        await repository.UpsertAsync(edited);

        (await File.ReadAllLinesAsync(fixture.Location.DeviceJournalPath)).Length.ShouldBe(2);
        (await repository.FindAsync(original.Id)).ShouldNotBeNull().Context.Note.ShouldBe("corrected");
    }

    [Fact]
    public async Task FindReturnsNullForUnknownId()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        (await repository.FindAsync(Guid.CreateVersion7())).ShouldBeNull();
    }

    [Fact]
    public async Task ReadsJournalsSyncedFromAnotherDevice()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var mine = ReadingFactory.Create(120, 80);
        await repository.UpsertAsync(mine);

        var theirs = ReadingFactory.Create(150, 95, TestClock.DefaultNow.AddHours(-1));
        fixture.WriteForeignJournal("phone01", ReadingLineSerializer.ToLine(theirs));

        var all = await repository.GetAllIncludingRetractedAsync();

        all.Count.ShouldBe(2);
        all.Select(reading => reading.Id).ShouldContain(theirs.Id);
    }

    [Fact]
    public async Task AnEditFromAnotherDeviceWinsWhenItIsNewer()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var mine = ReadingFactory.Create(120, 80);
        await repository.UpsertAsync(mine);

        var theirEdit = mine.WithContext(
            new MeasurementContext { Note = "edited on the phone" },
            TestClock.DefaultNow.AddHours(1));
        fixture.WriteForeignJournal("phone01", ReadingLineSerializer.ToLine(theirEdit));

        (await repository.FindAsync(mine.Id)).ShouldNotBeNull()
            .Context.Note.ShouldBe("edited on the phone");
    }

    [Fact]
    public async Task AStaleEditFromAnotherDeviceLoses()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var current = ReadingFactory.Create(120, 80, updatedAtUtc: TestClock.DefaultNow.AddHours(2));
        await repository.UpsertAsync(current);

        var stale = current.WithContext(
            new MeasurementContext { Note = "older" },
            TestClock.DefaultNow);
        fixture.WriteForeignJournal("phone01", ReadingLineSerializer.ToLine(stale));

        (await repository.FindAsync(current.Id)).ShouldNotBeNull().Context.Note.ShouldBeNull();
    }

    [Fact]
    public async Task ATruncatedForeignJournalDoesNotBreakLoading()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var good = ReadingFactory.Create(125, 82, TestClock.DefaultNow.AddHours(-2));
        var line = ReadingLineSerializer.ToLine(ReadingFactory.Create(140, 90));

        // A file copied mid-write ends with an incomplete line.
        fixture.WriteForeignJournal("phone01", ReadingLineSerializer.ToLine(good), line[..20]);

        var all = await repository.GetAllIncludingRetractedAsync();

        all.Count.ShouldBe(1);
        all[0].Id.ShouldBe(good.Id);
    }

    [Fact]
    public async Task GetRangeExcludesRetractedAndOrdersNewestFirst()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        foreach (var reading in ReadingFactory.CreateDailySeries(5))
        {
            await repository.UpsertAsync(reading);
        }

        var retracted = ReadingFactory.Create(180, 100, TestClock.DefaultNow.AddHours(-1));
        await repository.UpsertAsync(retracted);
        await repository.UpsertAsync(retracted.Retract(TestClock.DefaultNow.AddMinutes(1)));

        var window = await repository.GetRangeAsync(
            TestClock.DefaultNow.AddDays(-2),
            TestClock.DefaultNow);

        window.ShouldNotContain(reading => reading.IsDeleted);
        window.ShouldBeInOrder(SortDirection.Descending, Comparer<BloodPressureReading>.Create(
            (left, right) => left.MeasuredAt.CompareTo(right.MeasuredAt)));
    }

    [Fact]
    public async Task GetAllIncludingRetractedReturnsTombstones()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var reading = ReadingFactory.Create();
        await repository.UpsertAsync(reading);
        await repository.UpsertAsync(reading.Retract(TestClock.DefaultNow.AddMinutes(1)));

        var all = await repository.GetAllIncludingRetractedAsync();

        all.Count.ShouldBe(1);
        all[0].IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnsNothingWhenTheFolderIsEmpty()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        (await repository.GetAllIncludingRetractedAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangingTheFolderSwitchesToItsContents()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        await repository.UpsertAsync(ReadingFactory.Create());
        (await repository.GetAllIncludingRetractedAsync()).Count.ShouldBe(1);

        var elsewhere = Path.Combine(Path.GetTempPath(), $"bptracker-alt-{Guid.CreateVersion7()}");
        try
        {
            fixture.Location.SetDataFolder(elsewhere);
            (await repository.GetAllIncludingRetractedAsync()).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(elsewhere, recursive: true);
        }
    }

    [Fact]
    public async Task UpsertRejectsNull()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        await Should.ThrowAsync<ArgumentNullException>(() => repository.UpsertAsync(null!));
    }

    [Fact]
    public void ConstructorRejectsNullLocation() =>
        Should.Throw<ArgumentNullException>(() => new JournalReadingRepository(null!));
}
