namespace BPTracker.Presentation.Charts;

/// <summary>
/// The read-out shown while a finger rests on the chart: a vertical line at the nearest
/// measurement, plus the numbers and time to print beside it.
/// </summary>
public sealed record ChartCursor
{
    /// <summary>Where to draw the vertical line, snapped to the nearest measurement.</summary>
    public required double X { get; init; }

    /// <summary>The sample the line is snapped to.</summary>
    public required ChartSample Sample { get; init; }

    /// <summary>Marker on the systolic line.</summary>
    public required ChartPoint Systolic { get; init; }

    /// <summary>Marker on the diastolic line.</summary>
    public required ChartPoint Diastolic { get; init; }

    /// <summary>When the measurement was taken, formatted for display.</summary>
    public required string TimeText { get; init; }

    /// <summary>The two pressures, formatted for display.</summary>
    public required string ValueText { get; init; }

    /// <summary>The reading's tag, or <see langword="null"/> when it has none.</summary>
    public string? Tag { get; init; }
}
