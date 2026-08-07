namespace BPTracker.Presentation.Charts;

/// <summary>
/// Builds one plotted series, splitting the line exactly where it crosses its crisis threshold so
/// the crisis stretch can be drawn in a different colour from the rest.
/// </summary>
public static class ChartLineBuilder
{
    /// <summary>Builds one line.</summary>
    /// <param name="samples">Samples in time order.</param>
    /// <param name="select">Picks the pressure this line plots.</param>
    /// <param name="crisisFrom">The value at and above which the line is a crisis.</param>
    /// <param name="scale">Maps samples onto the plot area.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public static ChartLine Build(
        IReadOnlyList<ChartSample> samples,
        Func<ChartSample, int> select,
        int crisisFrom,
        ChartScale scale)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(scale);

        if (samples.Count == 0)
        {
            return ChartLine.Empty;
        }

        var dots = new List<ChartDot>(samples.Count);
        foreach (var sample in samples)
        {
            var value = select(sample);
            dots.Add(new ChartDot(
                scale.Point(sample.MeasuredAt, value),
                !string.IsNullOrWhiteSpace(sample.Tag),
                value >= crisisFrom));
        }

        var segments = new List<ChartSegment>(samples.Count);
        for (var index = 1; index < samples.Count; index++)
        {
            Append(
                segments,
                dots[index - 1].At,
                dots[index].At,
                select(samples[index - 1]),
                select(samples[index]),
                crisisFrom);
        }

        return new ChartLine(segments, dots);
    }

    private static void Append(
        List<ChartSegment> into,
        ChartPoint from,
        ChartPoint to,
        int fromValue,
        int toValue,
        int crisisFrom)
    {
        var crossing = Crossing(fromValue, toValue, crisisFrom);

        if (crossing is not { } fraction)
        {
            into.Add(new ChartSegment(from, to, fromValue >= crisisFrom));
            return;
        }

        var at = Between(from, to, fraction);
        into.Add(new ChartSegment(from, at, fromValue >= crisisFrom));
        into.Add(new ChartSegment(at, to, toValue >= crisisFrom));
    }

    private static double? Crossing(int fromValue, int toValue, int threshold)
    {
        if (fromValue == toValue || fromValue >= threshold == toValue >= threshold)
        {
            return null;
        }

        var fraction = (threshold - (double)fromValue) / (toValue - fromValue);
        return fraction is > 0 and < 1 ? fraction : null;
    }

    private static ChartPoint Between(ChartPoint from, ChartPoint to, double fraction) => new(
        from.X + ((to.X - from.X) * fraction),
        from.Y + ((to.Y - from.Y) * fraction));
}
