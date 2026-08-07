using System.Globalization;
using BPTracker.Application.Abstractions;
using BPTracker.Domain.Readings;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// Stores readings as append-only NDJSON journals in a folder an external tool syncs.
/// </summary>
/// <remarks>
/// <para>
/// Each device appends only to its own <c>readings-{deviceId}.ndjson</c> and reads every journal
/// in the folder. Because no two devices ever write the same file, the sync tool never has to
/// resolve a conflict, and a file copied mid-write only loses its last, incomplete line.
/// </para>
/// <para>
/// An edit appends a new line rather than rewriting the file; the highest
/// <see cref="BloodPressureReading.UpdatedAtUtc"/> wins on load.
/// </para>
/// </remarks>
public sealed class JournalReadingRepository : IReadingRepository, IDisposable
{
    private const string JournalPattern = "readings-*.ndjson";

    private readonly IStorageLocation _location;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<Guid, BloodPressureReading> _readings = [];

    // Null means "never loaded". An empty string is a valid signature for an empty folder,
    // so the two states must not share a value.
    private string? _loadedSignature;

    /// <summary>Creates the repository.</summary>
    public JournalReadingRepository(IStorageLocation location)
    {
        _location = location ?? throw new ArgumentNullException(nameof(location));
        _location.Changed += OnLocationChanged;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(BloodPressureReading reading, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reading);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await LoadAsync(cancellationToken).ConfigureAwait(false);

            var path = _location.DeviceJournalPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.AppendAllTextAsync(
                path,
                ReadingLineSerializer.ToLine(reading) + Environment.NewLine,
                cancellationToken).ConfigureAwait(false);

            Merge(reading);
            _loadedSignature = BuildSignature();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BloodPressureReading?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var all = await SnapshotAsync(cancellationToken).ConfigureAwait(false);
        return all.TryGetValue(id, out var reading) ? reading : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodPressureReading>> GetRangeAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default)
    {
        var all = await SnapshotAsync(cancellationToken).ConfigureAwait(false);

        return [.. all.Values
            .Where(reading => !reading.IsDeleted
                && reading.MeasuredAt >= fromInclusive
                && reading.MeasuredAt <= toInclusive)
            .OrderByDescending(reading => reading.MeasuredAt)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodPressureReading>> GetAllIncludingRetractedAsync(
        CancellationToken cancellationToken = default)
    {
        var all = await SnapshotAsync(cancellationToken).ConfigureAwait(false);
        return [.. all.Values.OrderByDescending(reading => reading.MeasuredAt)];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _location.Changed -= OnLocationChanged;
        _gate.Dispose();
    }

    private async Task<Dictionary<Guid, BloodPressureReading>> SnapshotAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await LoadAsync(cancellationToken).ConfigureAwait(false);
            return _readings;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var signature = BuildSignature();
        if (_loadedSignature is not null && signature == _loadedSignature)
        {
            return;
        }

        var merged = new Dictionary<Guid, BloodPressureReading>();

        foreach (var file in EnumerateJournals())
        {
            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // The sync tool may hold the file briefly. Skip it; the next read picks it up.
                continue;
            }

            foreach (var line in lines)
            {
                if (ReadingLineSerializer.TryParse(line, out var reading))
                {
                    Merge(merged, reading);
                }
            }
        }

        _readings = merged;
        _loadedSignature = signature;
    }

    private void Merge(BloodPressureReading reading) => Merge(_readings, reading);

    private static void Merge(Dictionary<Guid, BloodPressureReading> target, BloodPressureReading reading) =>
        target[reading.Id] = target.TryGetValue(reading.Id, out var existing)
            ? BloodPressureReading.ResolveConflict(existing, reading)
            : reading;

    private IEnumerable<string> EnumerateJournals()
    {
        var folder = _location.DataFolder;
        return Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, JournalPattern).Order(StringComparer.OrdinalIgnoreCase)
            : [];
    }

    /// <summary>
    /// Cheap fingerprint of every journal, so an edit made on another device is picked up
    /// without re-parsing the folder on every single query.
    /// </summary>
    private string BuildSignature()
    {
        var parts = EnumerateJournals().Select(file =>
        {
            var info = new FileInfo(file);
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{info.Name}:{info.Length}:{info.LastWriteTimeUtc.Ticks}");
        });

        return string.Join('|', parts);
    }

    private void OnLocationChanged(object? sender, EventArgs e) => _loadedSignature = null;
}
