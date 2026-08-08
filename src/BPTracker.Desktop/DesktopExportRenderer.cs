using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BPTracker.Presentation.Export;

namespace BPTracker.Desktop;

/// <summary>
/// Saves exports on Windows: the user picks the destination, and the images are rendered with WPF.
/// </summary>
public sealed class DesktopExportRenderer : IExportRenderer
{
    private const string PngFilter = "PNG image|*.png";

    /// <summary>The element photographed by <see cref="SaveChartImageAsync"/>, set once the window exists.</summary>
    public FrameworkElement? Chart { get; set; }

    public async Task<string?> SaveCsvAsync(string suggestedFileName, string csv, CancellationToken cancellationToken)
    {
        var path = Ask(suggestedFileName, "CSV file|*.csv");
        if (path is null)
        {
            return null;
        }

        await File.WriteAllTextAsync(path, csv, cancellationToken).ConfigureAwait(false);
        return path;
    }

    public async Task<string?> SaveChartImageAsync(string suggestedFileName, CancellationToken cancellationToken)
    {
        if (Chart is null || Chart.ActualWidth < 1 || Chart.ActualHeight < 1)
        {
            throw new IOException("The chart is not on screen yet.");
        }

        var path = Ask(suggestedFileName, PngFilter);
        if (path is null)
        {
            return null;
        }

        await WritePngAsync(path, Capture(Chart), cancellationToken).ConfigureAwait(false);
        return path;
    }

    public async Task<string?> SaveTableImageAsync(
        string suggestedFileName,
        ExportTable table,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(table);

        var path = Ask(suggestedFileName, PngFilter);
        if (path is null)
        {
            return null;
        }

        await WritePngAsync(path, ExportTableImage.Render(table), cancellationToken).ConfigureAwait(false);
        return path;
    }

    private static string? Ask(string suggestedFileName, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            DefaultExt = Path.GetExtension(suggestedFileName),
            AddExtension = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static RenderTargetBitmap Capture(FrameworkElement element)
    {
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight),
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(element);
        bitmap.Freeze();
        return bitmap;
    }

    private static async Task WritePngAsync(string path, BitmapSource bitmap, CancellationToken cancellationToken)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var buffer = new MemoryStream();
        encoder.Save(buffer);

        await File.WriteAllBytesAsync(path, buffer.ToArray(), cancellationToken).ConfigureAwait(false);
    }
}
