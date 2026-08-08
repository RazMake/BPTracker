using System.Globalization;
using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Export;

/// <summary>
/// The readings laid out as rows and columns, ready to be written as CSV or drawn as an image.
/// </summary>
/// <remarks>
/// Both exports and both apps share this one description of the table, so a column added here
/// appears everywhere and cannot drift between heads.
/// </remarks>
public sealed class ExportTable
{
    /// <summary>Height of a body row when drawn.</summary>
    public const float RowHeight = 24f;

    /// <summary>Height of the header row when drawn.</summary>
    public const float HeaderHeight = 30f;

    /// <summary>Blank space around the table when drawn.</summary>
    public const float Margin = 16f;

    /// <summary>Gap between a column's edge and its text.</summary>
    public const float CellPadding = 8f;

    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    private ExportTable(IReadOnlyList<IReadOnlyList<string>> rows) => Rows = rows;

    /// <summary>The columns, in order.</summary>
    public static IReadOnlyList<ExportColumn> Columns { get; } =
    [
        new("Date", 110),
        new("Time", 70),
        new("Sys", 60, AlignRight: true),
        new("Dia", 60, AlignRight: true),
        new("Tag", 260),
    ];

    /// <summary>Cell text for every reading, in column order.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

    /// <summary>Width of the drawn table, including margins.</summary>
    public static float Width { get; } = (2 * Margin) + Columns.Sum(column => column.Width);

    /// <summary>Height of the drawn table, including margins.</summary>
    public float Height => (2 * Margin) + HeaderHeight + (Rows.Count * RowHeight);

    /// <summary>Lays out readings oldest first, so the table reads in the same direction as the chart.</summary>
    public static ExportTable For(IEnumerable<BloodPressureReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        return new ExportTable([.. readings
            .OrderBy(reading => reading.MeasuredAt)
            .Select(Cells)]);
    }

    /// <summary>The left edge of a column, relative to the image.</summary>
    public static float LeftOf(int columnIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columnIndex, Columns.Count);

        var left = Margin;
        for (var index = 0; index < columnIndex; index++)
        {
            left += Columns[index].Width;
        }

        return left;
    }

    private static string[] Cells(BloodPressureReading reading)
    {
        var local = reading.MeasuredAt.LocalDateTime;

        return
        [
            local.ToString(DateFormat, CultureInfo.InvariantCulture),
            local.ToString(TimeFormat, CultureInfo.InvariantCulture),
            reading.Systolic.MmHg.ToString(CultureInfo.InvariantCulture),
            reading.Diastolic.MmHg.ToString(CultureInfo.InvariantCulture),
            reading.Context.Tag ?? string.Empty,
        ];
    }
}
