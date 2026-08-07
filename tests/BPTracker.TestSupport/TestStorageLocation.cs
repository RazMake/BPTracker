using BPTracker.Application.Abstractions;

namespace BPTracker.TestSupport;

/// <summary>
/// An in-memory <see cref="IStorageLocation"/> that touches no disk.
/// </summary>
public sealed class TestStorageLocation : IStorageLocation
{
    private string _dataFolder;

    /// <summary>Creates the fake, rooted at a fictional folder.</summary>
    public TestStorageLocation(string dataFolder = @"C:\Users\test\Documents\BPTracker") =>
        _dataFolder = dataFolder;

    /// <inheritdoc />
    public string DataFolder => _dataFolder;

    /// <inheritdoc />
    public string DeviceId => "test0001";

    /// <inheritdoc />
    public string DeviceJournalPath => Path.Combine(_dataFolder, $"readings-{DeviceId}.ndjson");

    /// <inheritdoc />
    public string? Problem { get; set; }

    /// <summary>When set, <see cref="SetDataFolder"/> throws this instead of succeeding.</summary>
    public Exception? FailWith { get; set; }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public void SetDataFolder(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (FailWith is not null)
        {
            throw FailWith;
        }

        _dataFolder = folder;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
