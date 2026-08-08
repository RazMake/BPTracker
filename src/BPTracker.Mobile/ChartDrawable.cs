using System.Globalization;
using BPTracker.Presentation.Charts;

namespace BPTracker.Mobile;

// Paints the frame the view model produces and nothing else. Every coordinate, colour decision
// and label in here was computed in BPTracker.Presentation, where the coverage gate can see it.
internal sealed class ChartDrawable(ChartViewModel viewModel) : IDrawable
{
    public const float LeftGutter = 36f;
    public const float RightPadding = 10f;
    public const float TopPadding = 12f;
    public const float BottomGutter = 24f;

    private static readonly Color Backdrop = Color.FromArgb("#0F1419");
    private static readonly Color GridLine = Color.FromArgb("#1C242D");
    private static readonly Color BandEdge = Color.FromArgb("#2C3742");
    private static readonly Color Muted = Color.FromArgb("#8A98A6");
    private static readonly Color SystolicLine = Color.FromArgb("#7EC8FF");
    private static readonly Color DiastolicLine = Color.FromArgb("#B79CFF");
    private static readonly Color CrisisLine = Color.FromArgb("#FF2D3E");
    private static readonly Color TaggedDot = Color.FromArgb("#FFD166");
    private static readonly Color NormalBand = Color.FromArgb("#1A3FBF77");
    private static readonly Color NormalBandLabel = Color.FromArgb("#7A3FBF77");
    private static readonly Color Highlight = Color.FromArgb("#E9EEF3");
    private static readonly Color Panel = Color.FromArgb("#181F27");

    private readonly ChartViewModel _viewModel = viewModel;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Backdrop;
        canvas.FillRectangle(dirtyRect);

        var frame = _viewModel.BuildFrame();
        if (!frame.HasData)
        {
            return;
        }

        // Every coordinate comes from the view model's plot size. Mixing in dirtyRect would put
        // the axis labels somewhere else during a layout pass, and leave them there.
        var plot = new RectF(0, 0, (float)_viewModel.PlotWidth, (float)_viewModel.PlotHeight);

        DrawBands(canvas, frame, plot.Width);
        DrawGrid(canvas, frame, plot.Width);
        DrawTimeLabels(canvas, frame, plot.Height);

        canvas.SaveState();
        canvas.ClipRectangle(LeftGutter, TopPadding, plot.Width, plot.Height);
        canvas.Translate(LeftGutter, TopPadding);

        DrawLine(canvas, frame.Systolic, SystolicLine);
        DrawLine(canvas, frame.Diastolic, DiastolicLine);
        DrawCursor(canvas, frame.Cursor, plot);

        canvas.RestoreState();
    }

    private static void DrawBands(ICanvas canvas, ChartFrame frame, float plotWidth)
    {
        const float labelHeight = 13f;

        canvas.FontSize = 10;

        foreach (var band in frame.Bands)
        {
            canvas.FillColor = NormalBand;
            canvas.FillRectangle(
                LeftGutter, TopPadding + (float)band.Top, plotWidth, (float)band.Height);

            if (band.Height < labelHeight * 2)
            {
                continue;
            }

            canvas.FontColor = NormalBandLabel;
            canvas.DrawString(
                band.Label,
                LeftGutter + 6, TopPadding + (float)band.Top + ((float)band.Height - labelHeight) / 2,
                plotWidth - 12, labelHeight,
                HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }

    private static void DrawGrid(ICanvas canvas, ChartFrame frame, float plotWidth)
    {
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 10;

        foreach (var line in frame.GridLines)
        {
            var y = TopPadding + (float)line.Y;
            canvas.StrokeColor = line.IsHealthyEdge ? BandEdge : GridLine;
            canvas.DrawLine(LeftGutter, y, LeftGutter + plotWidth, y);

            canvas.FontColor = Muted;
            canvas.DrawString(
                line.Value.ToString(CultureInfo.CurrentCulture),
                0, y - 7, LeftGutter - 5, 14,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }
    }

    private static void DrawTimeLabels(ICanvas canvas, ChartFrame frame, float plotHeight)
    {
        canvas.FontColor = Muted;
        canvas.FontSize = 10;

        foreach (var label in frame.TimeLabels)
        {
            canvas.DrawString(
                label.Text,
                LeftGutter + (float)label.X - 34, TopPadding + plotHeight + 5, 68, 14,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    private static void DrawLine(ICanvas canvas, ChartLine line, Color colour)
    {
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeDashPattern = null;

        foreach (var segment in line.Segments)
        {
            canvas.StrokeColor = segment.IsCritical ? CrisisLine : colour;
            canvas.DrawLine(
                (float)segment.From.X, (float)segment.From.Y,
                (float)segment.To.X, (float)segment.To.Y);
        }

        foreach (var dot in line.Dots)
        {
            canvas.FillColor = DotColour(dot, colour);
            canvas.FillCircle((float)dot.At.X, (float)dot.At.Y, dot.IsTagged ? 5.5f : 2.5f);
        }
    }

    private static Color DotColour(ChartDot dot, Color colour)
    {
        if (dot.IsTagged)
        {
            return TaggedDot;
        }

        return dot.IsCritical ? CrisisLine : colour;
    }

    private static void DrawCursor(ICanvas canvas, ChartCursor? cursor, RectF plot)
    {
        if (cursor is null)
        {
            return;
        }

        canvas.StrokeSize = 1;
        canvas.StrokeColor = Highlight;
        canvas.StrokeDashPattern = [3f, 3f];
        canvas.DrawLine((float)cursor.X, 0, (float)cursor.X, plot.Height);
        canvas.StrokeDashPattern = null;

        canvas.FillColor = Highlight;
        canvas.FillCircle((float)cursor.Systolic.X, (float)cursor.Systolic.Y, 4.5f);
        canvas.FillCircle((float)cursor.Diastolic.X, (float)cursor.Diastolic.Y, 4.5f);

        DrawReadout(canvas, cursor, plot);
    }

    // The read-out sits on whichever side of the line has room, so it never falls off the screen.
    private static void DrawReadout(ICanvas canvas, ChartCursor cursor, RectF plot)
    {
        const float width = 190f;
        var height = cursor.Tag is null ? 44f : 62f;

        var preferred = (float)cursor.X + 12;
        var left = preferred + width > plot.Width ? (float)cursor.X - 12 - width : preferred;
        left = Math.Clamp(left, 0, Math.Max(plot.Width - width, 0));

        canvas.FillColor = Panel;
        canvas.FillRoundedRectangle(left, 4, width, height, 8);
        canvas.StrokeColor = BandEdge;
        canvas.DrawRoundedRectangle(left, 4, width, height, 8);

        canvas.FontColor = Highlight;
        canvas.FontSize = 15;
        canvas.DrawString(cursor.ValueText, left + 10, 9, width - 20, 18,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        canvas.FontColor = Muted;
        canvas.FontSize = 11;
        canvas.DrawString(cursor.TimeText, left + 10, 28, width - 20, 16,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        if (cursor.Tag is not null)
        {
            canvas.FontColor = TaggedDot;
            canvas.DrawString(cursor.Tag, left + 10, 45, width - 20, 16,
                HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }
}
