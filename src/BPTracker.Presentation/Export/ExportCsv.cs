using System.Globalization;
using System.Text;

namespace BPTracker.Presentation.Export;

/// <summary>
/// Renders an <see cref="ExportTable"/> as CSV.
/// </summary>
public static class ExportCsv
{
    private const string FormulaLeadIns = "=+-@\t\r";

    /// <summary>Builds the whole file, header row included.</summary>
    public static string Build(ExportTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var builder = new StringBuilder();
        AppendRow(builder, ExportTable.Columns.Select(column => column.Header));

        foreach (var row in table.Rows)
        {
            AppendRow(builder, row);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<string> cells) =>
        builder.AppendLine(string.Join(',', cells.Select(Escape)));

    private static string Escape(string value)
    {
        // A tag beginning with one of these is a formula to a spreadsheet, so it is quoted and
        // prefixed rather than handed over as something to evaluate.
        var text = value.Length > 0 && FormulaLeadIns.Contains(value[0], StringComparison.Ordinal)
            ? "'" + value
            : value;

        return text.AsSpan().IndexOfAny(",\"\n\r'") >= 0
            ? string.Create(CultureInfo.InvariantCulture, $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"")
            : text;
    }
}
