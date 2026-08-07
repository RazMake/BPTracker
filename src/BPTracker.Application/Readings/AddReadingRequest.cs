using BPTracker.Domain.Readings;

namespace BPTracker.Application.Readings;

/// <summary>
/// Input for <see cref="AddReadingUseCase"/>.
/// </summary>
/// <param name="Systolic">Systolic pressure in mmHg.</param>
/// <param name="Diastolic">Diastolic pressure in mmHg.</param>
/// <param name="MeasuredAt">When the reading was taken. Defaults to now when omitted.</param>
/// <param name="Context">Optional circumstances of the measurement.</param>
public sealed record AddReadingRequest(
    int Systolic,
    int Diastolic,
    DateTimeOffset? MeasuredAt = null,
    MeasurementContext Context = default);
