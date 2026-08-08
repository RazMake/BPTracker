namespace BPTracker.Presentation.Export;

/// <summary>One column of an exported table, sized in device-independent pixels.</summary>
/// <param name="Header">Column heading, also the CSV header.</param>
/// <param name="Width">Column width when the table is drawn as an image.</param>
/// <param name="AlignRight">Whether cell text is right aligned, as numbers should be.</param>
public sealed record ExportColumn(string Header, float Width, bool AlignRight = false);
