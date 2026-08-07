using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BPTracker.Domain.Readings;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// One reading as it appears on a single line of a journal file.
/// </summary>
/// <remarks>
/// Property names are short but readable, because the user can open this file in a text editor.
/// </remarks>
public sealed record ReadingLine
{
    /// <summary>Reading identity.</summary>
    public Guid Id { get; init; }

    /// <summary>Systolic pressure in mmHg.</summary>
    public int Systolic { get; init; }

    /// <summary>Diastolic pressure in mmHg.</summary>
    public int Diastolic { get; init; }

    /// <summary>When it was measured, ISO-8601 with the original offset.</summary>
    public string MeasuredAt { get; init; } = string.Empty;

    /// <summary>Arm the cuff was on.</summary>
    public string Arm { get; init; } = nameof(MeasurementArm.Unspecified);

    /// <summary>Posture during measurement.</summary>
    public string Position { get; init; } = nameof(BodyPosition.Unspecified);

    /// <summary>Optional note.</summary>
    public string? Note { get; init; }

    /// <summary>When the record last changed, ISO-8601 UTC. Drives last-writer-wins.</summary>
    public string UpdatedAt { get; init; } = string.Empty;

    /// <summary>Whether the reading was retracted.</summary>
    public bool Deleted { get; init; }
}

[JsonSerializable(typeof(ReadingLine))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class ReadingJsonContext : JsonSerializerContext;

/// <summary>
/// Converts between <see cref="BloodPressureReading"/> and a journal line.
/// </summary>
public static class ReadingLineSerializer
{
    /// <summary>Renders a reading as a single JSON line, with no trailing newline.</summary>
    public static string ToLine(BloodPressureReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var line = new ReadingLine
        {
            Id = reading.Id,
            Systolic = reading.Systolic.MmHg,
            Diastolic = reading.Diastolic.MmHg,
            MeasuredAt = reading.MeasuredAt.ToString("O", CultureInfo.InvariantCulture),
            Arm = reading.Context.Arm.ToString(),
            Position = reading.Context.Position.ToString(),
            Note = reading.Context.Note,
            UpdatedAt = reading.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            Deleted = reading.IsDeleted,
        };

        return JsonSerializer.Serialize(line, ReadingJsonContext.Default.ReadingLine);
    }

    /// <summary>
    /// Parses a journal line.
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
            if (parsed is null || parsed.Id == Guid.Empty)
            {
                return false;
            }

            if (!SystolicPressure.TryFrom(parsed.Systolic, out var systolic) ||
                !DiastolicPressure.TryFrom(parsed.Diastolic, out var diastolic) ||
                systolic.MmHg <= diastolic.MmHg)
            {
                return false;
            }

            if (!TryParseInstant(parsed.MeasuredAt, out var measuredAt) ||
                !TryParseInstant(parsed.UpdatedAt, out var updatedAt))
            {
                return false;
            }

            var restored = BloodPressureReading.Create(
                systolic,
                diastolic,
                measuredAt,
                updatedAt,
                new MeasurementContext
                {
                    Arm = ParseEnum(parsed.Arm, MeasurementArm.Unspecified),
                    Position = ParseEnum(parsed.Position, BodyPosition.Unspecified),
                    Note = parsed.Note,
                },
                parsed.Id);

            reading = parsed.Deleted ? restored.Retract(updatedAt) : restored;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseInstant(string value, out DateTimeOffset instant) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out instant);

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}
