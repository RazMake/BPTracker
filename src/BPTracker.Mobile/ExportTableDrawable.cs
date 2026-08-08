using BPTracker.Presentation.Export;

namespace BPTracker.Mobile;

// Paints the table the Presentation layer laid out. Every column, width and cell string comes
// from ExportTable, so the phone and the desktop export the same picture.
internal sealed class ExportTableDrawable(ExportTable table) : IDrawable
{
    private static readonly Color Surface = Color.FromArgb("#181F27");
    private static readonly Color SurfaceAlt = Color.FromArgb("#0F1419");
    private static readonly Color Ink = Color.FromArgb("#E9EEF3");
    private static readonly Color Muted = Color.FromArgb("#8A98A6");
    private static readonly Color Line = Color.FromArgb("#2C3742");

    private const float FontSize = 12f;

    private readonly ExportTable _table = table;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Surface;
        canvas.FillRectangle(dirtyRect);
        canvas.FontSize = FontSize;

        DrawHeader(canvas);
        DrawRows(canvas);
    }

    private static void DrawHeader(ICanvas canvas)
    {
        var inner = ExportTable.Width - (2 * ExportTable.Margin);

        canvas.FillColor = SurfaceAlt;
        canvas.FillRectangle(ExportTable.Margin, ExportTable.Margin, inner, ExportTable.HeaderHeight);

        for (var column = 0; column < ExportTable.Columns.Count; column++)
        {
            DrawCell(canvas, ExportTable.Columns[column].Header, column, ExportTable.Margin, ExportTable.HeaderHeight, Muted);
        }

        var baseline = ExportTable.Margin + ExportTable.HeaderHeight;
        canvas.StrokeColor = Line;
        canvas.StrokeSize = 1;
        canvas.DrawLine(ExportTable.Margin, baseline, ExportTable.Width - ExportTable.Margin, baseline);
    }

    private void DrawRows(ICanvas canvas)
    {
        var inner = ExportTable.Width - (2 * ExportTable.Margin);
        var top = ExportTable.Margin + ExportTable.HeaderHeight;

        for (var row = 0; row < _table.Rows.Count; row++)
        {
            if (row % 2 == 1)
            {
                canvas.FillColor = SurfaceAlt;
                canvas.FillRectangle(ExportTable.Margin, top, inner, ExportTable.RowHeight);
            }

            for (var column = 0; column < ExportTable.Columns.Count; column++)
            {
                DrawCell(canvas, _table.Rows[row][column], column, top, ExportTable.RowHeight, Ink);
            }

            top += ExportTable.RowHeight;
        }
    }

    private static void DrawCell(ICanvas canvas, string text, int column, float top, float height, Color colour)
    {
        if (text.Length == 0)
        {
            return;
        }

        canvas.FontColor = colour;
        canvas.DrawString(
            text,
            ExportTable.LeftOf(column) + ExportTable.CellPadding,
            top,
            ExportTable.Columns[column].Width - (2 * ExportTable.CellPadding),
            height,
            ExportTable.Columns[column].AlignRight ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            VerticalAlignment.Center);
    }
}
