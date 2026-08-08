using BPTracker.Application.Abstractions;
using BPTracker.Presentation.Charts;
using BPTracker.Presentation.Export;
using Microsoft.Maui.Graphics.Platform;

namespace BPTracker.Mobile;

// Saves exports into the data folder, so they sit next to the journals and get picked up by
// whatever sync app the user pointed at that folder.
public sealed class MobileExportRenderer(IStorageLocation location, ChartViewModel chart) : IExportRenderer
{
    private const int ChartWidth = 1200;
    private const int ChartHeight = 700;

    private readonly IStorageLocation _location = location;
    private readonly ChartViewModel _chart = chart;

    public async Task<string?> SaveCsvAsync(string suggestedFileName, string csv, CancellationToken cancellationToken)
    {
        var path = Destination(suggestedFileName);
        await File.WriteAllTextAsync(path, csv, cancellationToken).ConfigureAwait(false);
        return path;
    }

    public async Task<string?> SaveChartImageAsync(string suggestedFileName, CancellationToken cancellationToken)
    {
        // Drawn off screen rather than captured, so an export does not depend on the chart tab
        // having been opened, or on how much room the phone gave it.
        await _chart.RefreshAsync(cancellationToken).ConfigureAwait(false);
        _chart.Resize(
            ChartWidth - ChartDrawable.LeftGutter - ChartDrawable.RightPadding,
            ChartHeight - ChartDrawable.TopPadding - ChartDrawable.BottomGutter);

        return Write(suggestedFileName, ChartWidth, ChartHeight, new ChartDrawable(_chart));
    }

    public Task<string?> SaveTableImageAsync(
        string suggestedFileName,
        ExportTable table,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(table);

        return Task.FromResult(Write(
            suggestedFileName,
            (int)Math.Ceiling(ExportTable.Width),
            (int)Math.Ceiling(table.Height),
            new ExportTableDrawable(table)));
    }

    private string? Write(string fileName, int width, int height, IDrawable drawable)
    {
        var bounds = new RectF(0, 0, width, height);

        using var context = new PlatformBitmapExportContext(width, height, 1f);
        drawable.Draw(context.Canvas, bounds);

        var path = Destination(fileName);
        using var stream = File.Create(path);
        context.WriteToStream(stream);
        return path;
    }

    private string Destination(string fileName)
    {
        Directory.CreateDirectory(_location.DataFolder);
        return Path.Combine(_location.DataFolder, fileName);
    }
}
