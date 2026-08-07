using BPTracker.Infrastructure.Storage;
using BPTracker.Infrastructure.Time;

namespace BPTracker.Infrastructure.Tests.Storage;

public sealed class StorageLocationTests
{
    [Fact]
    public void UsesTheDefaultFolderUntilTheUserChoosesAnother()
    {
        using var fixture = new StorageFixture();

        fixture.Location.DataFolder.ShouldBe(fixture.DataFolder);
    }

    [Fact]
    public void CreatesTheDataFolderSoTheUserCanFindIt()
    {
        using var fixture = new StorageFixture();

        Directory.Exists(fixture.Location.DataFolder).ShouldBeTrue();
    }

    [Fact]
    public void JournalNameIdentifiesThisDevice()
    {
        using var fixture = new StorageFixture();

        Path.GetFileName(fixture.Location.DeviceJournalPath)
            .ShouldBe($"readings-{fixture.Location.DeviceId}.ndjson");
    }

    [Fact]
    public void DeviceIdIsStableAcrossRestarts()
    {
        using var fixture = new StorageFixture();
        var first = fixture.Location.DeviceId;

        var restarted = new StorageLocation(fixture.SettingsFolder, fixture.DataFolder);

        restarted.DeviceId.ShouldBe(first);
    }

    [Fact]
    public void RemembersAFolderChosenByTheUser()
    {
        using var fixture = new StorageFixture();
        var chosen = Path.Combine(Path.GetTempPath(), $"bptracker-choice-{Guid.CreateVersion7()}");

        try
        {
            fixture.Location.SetDataFolder(chosen);

            var restarted = new StorageLocation(fixture.SettingsFolder, fixture.DataFolder);
            restarted.DataFolder.ShouldBe(Path.GetFullPath(chosen));
        }
        finally
        {
            Directory.Delete(chosen, recursive: true);
        }
    }

    [Fact]
    public void SettingsAreKeptOutOfTheSyncedFolder()
    {
        using var fixture = new StorageFixture();

        // Per-device settings must not travel between machines.
        Directory.GetFiles(fixture.Location.DataFolder).ShouldBeEmpty();
        File.Exists(Path.Combine(fixture.SettingsFolder, "storage.json")).ShouldBeTrue();
    }

    [Fact]
    public void ChangingTheFolderRaisesChanged()
    {
        using var fixture = new StorageFixture();
        var chosen = Path.Combine(Path.GetTempPath(), $"bptracker-choice-{Guid.CreateVersion7()}");
        var raised = false;
        fixture.Location.Changed += (_, _) => raised = true;

        try
        {
            fixture.Location.SetDataFolder(chosen);
            raised.ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(chosen, recursive: true);
        }
    }

    [Fact]
    public void SettingTheSameFolderIsANoOp()
    {
        using var fixture = new StorageFixture();
        var raised = false;
        fixture.Location.Changed += (_, _) => raised = true;

        fixture.Location.SetDataFolder(fixture.Location.DataFolder);

        raised.ShouldBeFalse();
    }

    [Fact]
    public void RecoversFromADamagedSettingsFile()
    {
        using var fixture = new StorageFixture();
        File.WriteAllText(Path.Combine(fixture.SettingsFolder, "storage.json"), "{ not json");

        var restarted = new StorageLocation(fixture.SettingsFolder, fixture.DataFolder);

        restarted.DeviceId.ShouldNotBeNullOrWhiteSpace();
        restarted.DataFolder.ShouldBe(fixture.DataFolder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsABlankFolder(string folder)
    {
        using var fixture = new StorageFixture();

        Should.Throw<ArgumentException>(() => fixture.Location.SetDataFolder(folder));
    }

    [Fact]
    public void ConstructorRejectsBlankArguments()
    {
        Should.Throw<ArgumentException>(() => new StorageLocation("", "data"));
        Should.Throw<ArgumentException>(() => new StorageLocation("settings", ""));
    }
}

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNowIsInUtc() => new SystemClock().UtcNow.Offset.ShouldBe(TimeSpan.Zero);

    [Fact]
    public void UtcNowIsCloseToTheRealClock() =>
        new SystemClock().UtcNow.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

    [Fact]
    public void LocalNowUsesTheDeviceOffset() =>
        new SystemClock().LocalNow.Offset.ShouldBe(DateTimeOffset.Now.Offset);
}
