namespace BPTracker.Application.Abstractions;

/// <summary>
/// Where readings are kept on disk.
/// </summary>
/// <remarks>
/// The folder is deliberately user-visible and user-changeable: an external tool
/// (OneDrive, Syncthing, Google Drive) is what moves data between devices, so the user has to be
/// able to see the path and point it at whatever folder that tool watches.
/// </remarks>
public interface IStorageLocation
{
    /// <summary>The folder holding every device's journal. Shown to the user.</summary>
    string DataFolder { get; }

    /// <summary>Full path of the journal this device appends to. No other device writes it.</summary>
    string DeviceJournalPath { get; }

    /// <summary>Stable identifier for this device, used in the journal file name.</summary>
    string DeviceId { get; }

    /// <summary>
    /// Why the folder is unusable, or <see langword="null"/> when it is fine.
    /// </summary>
    /// <remarks>
    /// Android scoped storage can refuse the default Documents path until the user grants
    /// permission. That has to surface as a message the user can act on, never as a crash.
    /// </remarks>
    string? Problem { get; }

    /// <summary>Points storage at a different folder and remembers the choice.</summary>
    /// <exception cref="ArgumentException">The path is blank.</exception>
    void SetDataFolder(string folder);

    /// <summary>Raised after <see cref="SetDataFolder"/> changes the location.</summary>
    event EventHandler? Changed;
}
