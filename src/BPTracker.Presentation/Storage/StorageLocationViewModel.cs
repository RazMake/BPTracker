using BPTracker.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BPTracker.Presentation.Storage;

/// <summary>
/// Surfaces the data folder so the user can see it, and change it to wherever their
/// sync tool watches.
/// </summary>
public sealed partial class StorageLocationViewModel : ObservableObject
{
    private readonly IStorageLocation _location;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Creates the view model.</summary>
    public StorageLocationViewModel(IStorageLocation location)
    {
        _location = location ?? throw new ArgumentNullException(nameof(location));
        _location.Changed += OnLocationChanged;
        StatusMessage = _location.Problem;
    }

    /// <summary>The folder holding every device's journal.</summary>
    public string DataFolder => _location.DataFolder;

    /// <summary>The file this device writes. Other devices never touch it.</summary>
    public string DeviceJournalPath => _location.DeviceJournalPath;

    /// <summary>This device's short identifier, which appears in the journal file name.</summary>
    public string DeviceId => _location.DeviceId;

    /// <summary>Raised after the folder changes, so the host can reload.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Points storage at another folder.
    /// </summary>
    /// <returns><see langword="true"/> when the folder was changed.</returns>
    public bool ChangeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return false;
        }

        try
        {
            _location.SetDataFolder(folder);
            StatusMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StatusMessage = $"Could not use that folder: {exception.Message}";
            return false;
        }
    }

    /// <summary>Detaches the location event handler.</summary>
    public void Detach() => _location.Changed -= OnLocationChanged;

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(DataFolder));
        OnPropertyChanged(nameof(DeviceJournalPath));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
