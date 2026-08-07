using System.Text.Json;
using System.Text.Json.Serialization;
using BPTracker.Application.Abstractions;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// Resolves the data folder, remembering a user override.
/// </summary>
/// <remarks>
/// The override and the device id live in app-private storage, never in the synced folder:
/// they are per-device settings and must not travel between machines.
/// </remarks>
public sealed class StorageLocation : IStorageLocation
{
    private const string SettingsFileName = "storage.json";

    private readonly string _settingsFolder;
    private readonly string _defaultDataFolder;
    private StorageSettings _settings;

    /// <summary>Creates the location resolver.</summary>
    /// <param name="settingsFolder">App-private folder for per-device settings.</param>
    /// <param name="defaultDataFolder">Folder to use until the user picks another.</param>
    public StorageLocation(string settingsFolder, string defaultDataFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultDataFolder);

        _settingsFolder = settingsFolder;
        _defaultDataFolder = defaultDataFolder;
        _settings = Load();

        TryCreateDataFolder();
    }

    /// <summary>
    /// Why the data folder is not usable, or <see langword="null"/> when it is fine.
    /// </summary>
    /// <remarks>
    /// Android scoped storage can refuse the default Documents path until the user grants
    /// permission. That must surface as a message the user can act on, never as a crash on start.
    /// </remarks>
    public string? Problem { get; private set; }

    /// <inheritdoc />
    public string DataFolder => _settings.DataFolder ?? _defaultDataFolder;

    /// <inheritdoc />
    public string DeviceId => _settings.DeviceId;

    /// <inheritdoc />
    public string DeviceJournalPath => Path.Combine(DataFolder, $"readings-{DeviceId}.ndjson");

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public void SetDataFolder(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        var resolved = Path.GetFullPath(folder);
        if (string.Equals(resolved, DataFolder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Directory.CreateDirectory(resolved);
        _settings = _settings with { DataFolder = resolved };
        Save();
        Problem = null;

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private string SettingsPath => Path.Combine(_settingsFolder, SettingsFileName);

    private void TryCreateDataFolder()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            Problem = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Problem = $"Cannot use {DataFolder}: {exception.Message}";
        }
    }

    private StorageSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize(
                    File.ReadAllText(SettingsPath),
                    StorageJsonContext.Default.StorageSettings);

                if (loaded is not null && !string.IsNullOrWhiteSpace(loaded.DeviceId))
                {
                    return loaded;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // A damaged settings file must not stop the app; fall back to a fresh identity.
        }

        var created = new StorageSettings(NewDeviceId(), null);
        _settings = created;
        Save();
        return created;
    }

    private void Save()
    {
        Directory.CreateDirectory(_settingsFolder);
        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(_settings, StorageJsonContext.Default.StorageSettings));
    }

    // Short, filename-safe, and not derived from anything identifying.
    private static string NewDeviceId() =>
        Guid.CreateVersion7().ToString("N", System.Globalization.CultureInfo.InvariantCulture)[..8];
}

/// <summary>Per-device settings persisted outside the synced folder.</summary>
/// <param name="DeviceId">Stable id for this device.</param>
/// <param name="DataFolder">User override, or <see langword="null"/> to use the default.</param>
public sealed record StorageSettings(string DeviceId, string? DataFolder);

[JsonSerializable(typeof(StorageSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class StorageJsonContext : JsonSerializerContext;
