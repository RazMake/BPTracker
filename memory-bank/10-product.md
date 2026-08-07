# 10 - Product

## What this is

A personal blood pressure log with two clients over one shared core.

| Client | Job |
| --- | --- |
| Android phone | **Capture.** Enter two numbers in under five seconds, one handed. |
| Windows desktop | **Review.** Read the history and see the trend over time comfortably. |

The asymmetry is deliberate. The phone optimises for speed of entry and shows almost nothing.
The desktop optimises for reading and does not need to be fast to type into.

## Non-negotiable UX rules

### Phone (entry)
- Systolic and diastolic are the only required inputs. Nothing else may block a save.
- Fields start seeded from the previous reading, because consecutive readings are close.
- Category feedback appears live, as the numbers change, before saving.
- Everything optional (arm, position, note) is collapsed by default.
- One tap saves. No confirmation dialog.

### Desktop (review)
- The trend chart is the primary element, not an afterthought.
- Raw daily averages and a smoothed moving average are both drawn, so noise is visible
  without hiding the signal.
- The history list is always reachable without navigating away from the chart.

## Explicit non-goals

- **Pulse is not tracked.** Decided by the product owner. Do not add it back without being asked.
- No medical advice, diagnosis or treatment suggestions. Categories are informational labels only.
- No accounts, no multi-user support. This is a single person's data.
- No analytics or telemetry. See the privacy note below.

## Privacy

Blood pressure readings are health data.

- Never log reading values, even at debug level.
- No third-party analytics, crash reporting with payloads, or ad SDKs.
- Data stays in a folder the user chooses. The app itself never transmits anything.

## Sync

The app does not sync. The user points both apps at a folder their own sync tool watches.
See [80-storage-and-sync.md](80-storage-and-sync.md).
