using BPTracker.Domain.Readings;
using BPTracker.Infrastructure.Storage;
using BPTracker.TestSupport;

namespace BPTracker.Infrastructure.Tests.Storage;

public sealed class JournalReadingRepositoryTests
{
    private const string LegacyLine =
        """
        {"Id":"0197f0a0-0000-7000-8000-000000000001","Systolic":128,"Diastolic":82,"MeasuredAt":"2026-05-04T09:15:00.0000000+00:00","Arm":"Left","Position":"Sitting","Note":"after coffee","UpdatedAt":"2026-05-04T09:20:00.0000000+00:00","Deleted":false}
        """;

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
            new MeasurementContext { Tag = "corrected" },
            TestClock.DefaultNow.AddMinutes(10));
        await repository.UpsertAsync(edited);

        (await File.ReadAllLinesAsync(fixture.Location.DeviceJournalPath)).Length.ShouldBe(2);
        (await repository.FindAsync(original.Id)).ShouldNotBeNull().Context.Tag.ShouldBe("corrected");
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
            new MeasurementContext { Tag = "edited on the phone" },
            TestClock.DefaultNow.AddHours(1));
        fixture.WriteForeignJournal("phone01", ReadingLineSerializer.ToLine(theirEdit));

        (await repository.FindAsync(mine.Id)).ShouldNotBeNull()
            .Context.Tag.ShouldBe("edited on the phone");
    }

    [Fact]
    public async Task AStaleEditFromAnotherDeviceLoses()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();

        var current = ReadingFactory.Create(120, 80, updatedAtUtc: TestClock.DefaultNow.AddHours(2));
        await repository.UpsertAsync(current);

        var stale = current.WithContext(
            new MeasurementContext { Tag = "older" },
            TestClock.DefaultNow);
        fixture.WriteForeignJournal("phone01", ReadingLineSerializer.ToLine(stale));

        (await repository.FindAsync(current.Id)).ShouldNotBeNull().Context.Tag.ShouldBeNull();
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
    public async Task RewritesThisDevicesJournalIntoTheCurrentShape()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();
        fixture.WriteOwnJournal(LegacyLine);

        var all = await repository.GetAllIncludingRetractedAsync();

        all.Count.ShouldBe(1);
        var migrated = await File.ReadAllTextAsync(fixture.Location.DeviceJournalPath);
        migrated.ShouldContain("\"Sys\":128");
        migrated.ShouldNotContain("Systolic");
        migrated.ShouldNotContain("MeasuredAt");
    }

    [Fact]
    public async Task LeavesAnotherDevicesJournalExactlyAsItArrived()
    {
        using var fixture = new StorageFixture();
        using var repository = fixture.CreateRepository();
        await repository.UpsertAsync(ReadingFactory.Create());
        fixture.WriteForeignJournal("phone01", LegacyLine);
        var foreign = Path.Combine(fixture.Location.DataFolder, "readings-phone01.ndjson");

        (await repository.GetAllIncludingRetractedAsync()).Count.ShouldBe(2);

        (await File.ReadAllLinesAsync(foreign)).ShouldBe([LegacyLine]);
    }

    [Fact]
    public async Task LeavesAJournalAlreadyInTheCurrentShapeUntouched()
    {
        using var fixture = new StorageFixture();
        using var first = fixture.CreateRepository();
        await first.UpsertAsync(ReadingFactory.Create());
        var before = await File.ReadAllTextAsync(fixture.Location.DeviceJournalPath);

        using var second = fixture.CreateRepository();
        await second.GetAllIncludingRetractedAsync();

        (await File.ReadAllTextAsync(fixture.Location.DeviceJournalPath)).ShouldBe(before);
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
