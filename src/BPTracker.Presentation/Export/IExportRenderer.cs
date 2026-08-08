namespace BPTracker.Presentation.Export;

/// <summary>
/// Turns an export into a file. Implemented by each app, because only the app knows how to draw
/// its own chart and where the user wants the file put.
/// </summary>
/// <remarks>
/// Every method returns the full path of the file written, or <see langword="null"/> when the user
/// backed out of a save dialog. Anything else is a failure and should be thrown.
/// </remarks>
public interface IExportRenderer
{
    /// <summary>Writes the CSV text.</summary>
    Task<string?> SaveCsvAsync(string suggestedFileName, string csv, CancellationToken cancellationToken);

    /// <summary>Writes a PNG of the trend chart exactly as it is currently drawn.</summary>
    Task<string?> SaveChartImageAsync(string suggestedFileName, CancellationToken cancellationToken);

    /// <summary>Writes a PNG of the readings table.</summary>
    Task<string?> SaveTableImageAsync(string suggestedFileName, ExportTable table, CancellationToken cancellationToken);
}
