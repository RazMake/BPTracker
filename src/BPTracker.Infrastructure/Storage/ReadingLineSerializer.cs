using System.Globalization;
using System.Text.Json;
using BPTracker.Domain.Readings;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// Converts between <see cref="BloodPressureReading"/> and a journal line.
/// </summary>
public static class ReadingLineSerializer
{
    /// <summary>Time used when a line carries a date but no time.</summary>
    /// <remarks>
    /// A reading with no time is almost always the morning one, so guessing is more useful than
    /// dropping the line.
    /// </remarks>
    public const string DefaultTime = "07:30";

    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    /// <summary>Renders a reading as a single JSON line, with no trailing newline.</summary>
    public static string ToLine(BloodPressureReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        // The wall clock the user saw, not the stored offset: a date and a time cannot carry an
        // offset, so a reading round-trips against this device's own zone.
        var local = reading.MeasuredAt.LocalDateTime;

        var line = new ReadingLine
        {
            Date = local.ToString(DateFormat, CultureInfo.InvariantCulture),
            Time = local.ToString(TimeFormat, CultureInfo.InvariantCulture),
            Sys = reading.Systolic.MmHg,
            Dia = reading.Diastolic.MmHg,
            Tag = reading.Context.Tag,
            Id = reading.Id,
            UpdatedAt = reading.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            Deleted = reading.IsDeleted,
        };

        return JsonSerializer.Serialize(line, ReadingJsonContext.Default.ReadingLine);
    }

    /// <summary>
    /// Parses a journal line, in either the current shape or the one written before it.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> for a blank, malformed or implausible line. A partially synced file
    /// can end mid-line, so an unreadable line is expected and must never throw.
    /// </returns>
    public static bool TryParse(string line, out BloodPressureReading reading)
    {
        reading = null!;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize(line, ReadingJsonContext.Default.ReadingLine);
            if (parsed is null)
            {
                return false;
            }

            // A legacy line has no Date, so it arrives here empty rather than wrong.
            return string.IsNullOrEmpty(parsed.Date)
                ? TryParseLegacy(line, out reading)
                : TryBuild(parsed, out reading);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseLegacy(string line, out BloodPressureReading reading)
    {
        reading = null!;

        var parsed = JsonSerializer.Deserialize(line, LegacyReadingJsonContext.Default.LegacyReadingLine);

        return parsed is not null
            && TryParseInstant(parsed.MeasuredAt, out var measuredAt)
            && TryBuild(Reshape(parsed, measuredAt), out reading);
    }

    private static ReadingLine Reshape(LegacyReadingLine parsed, DateTimeOffset measuredAt)
    {
        var local = measuredAt.LocalDateTime;

        return new ReadingLine
        {
            Date = local.ToString(DateFormat, CultureInfo.InvariantCulture),
            Time = local.ToString(TimeFormat, CultureInfo.InvariantCulture),
            Sys = parsed.Systolic,
            Dia = parsed.Diastolic,
            Tag = parsed.Note,
            Id = parsed.Id,
            UpdatedAt = parsed.UpdatedAt,
            Deleted = parsed.Deleted,
        };
    }

    private static bool TryBuild(ReadingLine parsed, out BloodPressureReading reading)
    {
        reading = null!;

        if (parsed.Id == Guid.Empty ||
            !SystolicPressure.TryFrom(parsed.Sys, out var systolic) ||
            !DiastolicPressure.TryFrom(parsed.Dia, out var diastolic) ||
            systolic.MmHg <= diastolic.MmHg ||
            !TryParseMeasuredAt(parsed.Date, parsed.Time, out var measuredAt) ||
            !TryParseInstant(parsed.UpdatedAt, out var updatedAt))
        {
            return false;
        }

        var restored = BloodPressureReading.Create(
            systolic,
            diastolic,
            measuredAt,
            updatedAt,

            // Clamped rather than validated: a journal written before the limit shrank must still
            // load, and Create rejects an over-length tag outright.
            new MeasurementContext { Tag = MeasurementContext.Clamp(parsed.Tag) },
            parsed.Id);

        reading = parsed.Deleted ? restored.Retract(updatedAt) : restored;
        return true;
    }

    private static bool TryParseMeasuredAt(string? date, string? time, out DateTimeOffset measuredAt)
    {
        measuredAt = default;

        if (!DateOnly.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return false;
        }

        var text = string.IsNullOrWhiteSpace(time) ? DefaultTime : time.Trim();
        if (!TimeOnly.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock))
        {
            return false;
        }

        var local = day.ToDateTime(clock, DateTimeKind.Unspecified);
        measuredAt = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
        return true;
    }

    private static bool TryParseInstant(string? value, out DateTimeOffset instant) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out instant);
}
