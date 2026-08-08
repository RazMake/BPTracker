using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BPTracker.Presentation.Export;

namespace BPTracker.Desktop;

/// <summary>
/// Draws an <see cref="ExportTable"/> onto a bitmap, in the same palette as the app.
/// </summary>
internal static class ExportTableImage
{
    private static readonly Brush Surface = Frozen(0x18, 0x1F, 0x27);
    private static readonly Brush SurfaceAlt = Frozen(0x0F, 0x14, 0x19);
    private static readonly Brush Ink = Frozen(0xE9, 0xEE, 0xF3);
    private static readonly Brush InkMuted = Frozen(0x8A, 0x98, 0xA6);
    private static readonly Pen Line = FrozenPen(0x2C, 0x37, 0x42);
    private static readonly Typeface Face = new("Segoe UI");

    internal static BitmapSource Render(ExportTable table)
    {
        var width = ExportTable.Width;
        var height = table.Height;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Surface, null, new Rect(0, 0, width, height));
            DrawHeader(context, width);
            DrawRows(context, table, width);
        }

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(width),
            (int)Math.Ceiling(height),
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawHeader(DrawingContext context, double width)
    {
        var top = (double)ExportTable.Margin;

        context.DrawRectangle(
            SurfaceAlt,
            null,
            new Rect(ExportTable.Margin, top, width - (2 * ExportTable.Margin), ExportTable.HeaderHeight));

        for (var index = 0; index < ExportTable.Columns.Count; index++)
        {
            DrawCell(context, ExportTable.Columns[index].Header, index, top, ExportTable.HeaderHeight, InkMuted);
        }

        var baseline = top + ExportTable.HeaderHeight;
        context.DrawLine(
            Line,
            new Point(ExportTable.Margin, baseline),
            new Point(width - ExportTable.Margin, baseline));
    }

    private static void DrawRows(DrawingContext context, ExportTable table, double width)
    {
        var top = ExportTable.Margin + (double)ExportTable.HeaderHeight;

        for (var row = 0; row < table.Rows.Count; row++)
        {
            if (row % 2 == 1)
            {
                context.DrawRectangle(
                    SurfaceAlt,
                    null,
                    new Rect(ExportTable.Margin, top, width - (2 * ExportTable.Margin), ExportTable.RowHeight));
            }

            for (var column = 0; column < ExportTable.Columns.Count; column++)
            {
                DrawCell(context, table.Rows[row][column], column, top, ExportTable.RowHeight, Ink);
            }

            top += ExportTable.RowHeight;
        }
    }

    private static void DrawCell(
        DrawingContext context,
        string text,
        int column,
        double top,
        double rowHeight,
        Brush brush)
    {
        if (text.Length == 0)
        {
            return;
        }

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Face,
            12,
            brush,
            1.0)
        {
            MaxTextWidth = ExportTable.Columns[column].Width - (2 * ExportTable.CellPadding),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        var left = ExportTable.LeftOf(column) + ExportTable.CellPadding;
        if (ExportTable.Columns[column].AlignRight)
        {
            left += (float)(formatted.MaxTextWidth - formatted.Width);
        }

        context.DrawText(formatted, new Point(left, top + ((rowHeight - formatted.Height) / 2)));
    }

    private static SolidColorBrush Frozen(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen(byte red, byte green, byte blue)
    {
        var pen = new Pen(Frozen(red, green, blue), 1);
        pen.Freeze();
        return pen;
    }
}
