using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation.Export;

/// <summary>
/// Drives the three exports: the readings as CSV, the chart as a picture, and the readings as a
/// picture of a table.
/// </summary>
/// <remarks>
/// Both apps share this, so an export behaves the same on the phone and on the desktop. Only the
/// drawing and the choice of destination differ, and those sit behind <see cref="IExportRenderer"/>.
/// </remarks>
public sealed partial class ExportViewModel : ObservableObject
{
    /// <summary>How far back an export reaches. Long enough to mean "everything recorded".</summary>
    public const int WindowDays = 3650;

    private const string NothingToExport = "There is nothing to export yet.";

    private readonly GetReadingHistoryUseCase _getHistory;
    private readonly IExportRenderer _renderer;
    private readonly IClock _clock;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Creates the view model.</summary>
    public ExportViewModel(GetReadingHistoryUseCase getHistory, IExportRenderer renderer, IClock clock)
    {
        _getHistory = getHistory ?? throw new ArgumentNullException(nameof(getHistory));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Writes every reading as CSV.</summary>
    [RelayCommand]
    public Task ExportCsvAsync(CancellationToken cancellationToken) =>
        RunAsync(
            (table, token) => _renderer.SaveCsvAsync(FileName("readings", "csv"), ExportCsv.Build(table), token),
            cancellationToken);

    /// <summary>Writes the chart, as it is currently drawn, as a PNG.</summary>
    [RelayCommand]
    public Task ExportChartAsync(CancellationToken cancellationToken) =>
        RunAsync((_, token) => _renderer.SaveChartImageAsync(FileName("chart", "png"), token), cancellationToken);

    /// <summary>Writes every reading as a PNG of a table.</summary>
    [RelayCommand]
    public Task ExportDataAsync(CancellationToken cancellationToken) =>
        RunAsync(
            (table, token) => _renderer.SaveTableImageAsync(FileName("readings", "png"), table, token),
            cancellationToken);

    private string FileName(string kind, string extension) =>
        $"bptracker-{kind}-{_clock.UtcNow.LocalDateTime:yyyyMMdd-HHmm}.{extension}";

    private async Task RunAsync(
        Func<ExportTable, CancellationToken, Task<string?>> save,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var readings = await _getHistory.ExecuteAsync(WindowDays, cancellationToken).ConfigureAwait(true);
            var table = ExportTable.For(readings);

            if (table.Rows.Count == 0)
            {
                StatusMessage = NothingToExport;
                return;
            }

            var path = await save(table, cancellationToken).ConfigureAwait(true);
            StatusMessage = path is null ? "Export cancelled." : $"Saved {Path.GetFileName(path)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not export: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
