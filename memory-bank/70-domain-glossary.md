# 70 - Domain glossary

| Term | Meaning |
| --- | --- |
| **Systolic** | The higher number. Pressure while the heart contracts. Plausible range 50-300 mmHg. |
| **Diastolic** | The lower number. Pressure between beats. Plausible range 30-200 mmHg. |
| **mmHg** | Millimetres of mercury. The unit for both numbers. |
| **Pulse pressure** | Systolic minus diastolic. |
| **MAP** | Mean arterial pressure, estimated as `diastolic + (pulse pressure / 3)`. |
| **Reading** | One measurement: two pressures, a timestamp, and optional context. |
| **Retracted** | Soft-deleted. The record stays as a tombstone so the deletion can sync. |
| **Measurement context** | Optional circumstances: arm, body position, tag. |
| **Tag** | Optional one-line label on a reading, at most 100 characters, for the rare measurement that needs explaining. Written as `Tag` on disk. |

## Categories (ACC/AHA 2017)

Evaluated **most severe first**, because the bands overlap.

| Category | Condition |
| --- | --- |
| Hypertensive crisis | systolic >= 181 **or** diastolic >= 121 |
| Hypotension | systolic < 90 **or** diastolic < 60 |
| Hypertension stage 2 | systolic >= 140 **or** diastolic >= 90 |
| Hypertension stage 1 | systolic >= 130 **or** diastolic >= 80 |
| Elevated | systolic 120-129 **and** diastolic < 80 |
| Normal | systolic < 120 **and** diastolic < 80 |

Implemented in `BloodPressureClassifier`. **Order of the checks is load-bearing** - reordering them
changes results for overlapping readings. The tests pin the boundaries; if you change a threshold,
expect them to fail and update them deliberately.

These labels are informational. They are never presented as diagnosis or advice.

## Trend terms

| Term | Meaning |
| --- | --- |
| **Daily average** | All readings on one local calendar day, averaged into one point. |
| **Moving average** | Trailing simple average over N daily points. Leading points average over fewer samples rather than being dropped, so the smoothed series stays aligned with the raw series on the x-axis. |
| **Trend period** | The window one page covers: week (7d), month (30d), quarter (90d) or year (365d). A year is the most the chart ever loads; older history is paged. |
