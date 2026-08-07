using BPTracker.Infrastructure.Storage;

namespace BPTracker.Infrastructure.Tests.Storage;

/// <summary>
/// Gives each test its own temporary data folder and settings folder.
/// </summary>
public sealed class StorageFixture : IDisposable
{
    private readonly string _root;

    public StorageFixture()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bptracker-tests-{Guid.CreateVersion7()}");
        SettingsFolder = Path.Combine(_root, "settings");
        DataFolder = Path.Combine(_root, "data");

        Location = new StorageLocation(SettingsFolder, DataFolder);
    }

    public string SettingsFolder { get; }

    public string DataFolder { get; }

    public StorageLocation Location { get; }

    public JournalReadingRepository CreateRepository() => new(Location);

    /// <summary>Simulates a journal arriving from another device via the sync tool.</summary>
    public void WriteForeignJournal(string deviceId, params string[] lines) =>
        File.WriteAllLines(Path.Combine(Location.DataFolder, $"readings-{deviceId}.ndjson"), lines);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
