# 10 - Product

## What this is

A personal blood pressure log with two clients over one shared core.

| Client | Job |
| --- | --- |
| Android phone | **Capture**, plus a chart to glance at. Enter two numbers in seconds, one handed. |
| Windows desktop | **Review.** Read the history and see the trend over time comfortably. |

The asymmetry is deliberate. The phone optimises for speed of entry and shows almost nothing on
the entry screen. The desktop optimises for reading and does not need to be fast to type into.

## Non-negotiable UX rules

### Phone (entry)
- Systolic and diastolic are the only inputs that matter. Arm and position are not asked for at
  all: they were never used, and every control on this screen costs entry speed.
- A **tag** is the one exception: a single quiet line, at most 100 characters, for the rare reading
  that needs explaining. It must never draw attention away from the two numbers.
- Each number is changed by **sliding a finger across it**, not by tapping a step button once per
  mmHg. Tapping the number opens the keypad for a large jump.
- Fields start seeded from the previous reading, because consecutive readings are close.
- Category feedback appears live, as the numbers change, before saving.
- One tap saves. No confirmation dialog and no "Saved" message: the button simply **disables until
  a number changes**, which both confirms the save and prevents recording it twice.
- **Reset** sits beside Save and puts both numbers back to 120/80.
- A number at or above its crisis threshold is shown in **bright red**.
- Red text appears only when something is wrong. Nothing else competes for attention.
- The app is **dark only**, and set explicitly rather than following the system.
- Both orientations work: the two numbers and the save panel stack in portrait and sit side by
  side in landscape.

### Chart (both apps)
- Every measurement, both series, lines rather than dots.
- Time is proportional: a week without a reading is a week of empty chart.
- The background is shaded pale green **only** where each series is normal, and every band is
  labelled with the series it applies to. Shading the bad ranges too was tried and covered most of
  the chart, which left the shading meaning nothing.
- A **crisis is called out on the line**, in bright red, from the exact point it crosses the
  threshold. Neither series' own colour may be red, or the two would be confusable.
- A **tagged reading is a larger amber dot**. On the desktop, hovering it shows the tag; on the
  phone the tag appears in the read-out when the vertical line is on that reading.
- The phone and the desktop use the same palette and the same shaded corridors.
  See [ADR-0004](decisions/ADR-0004-chart-shading.md).

### Chart (phone only)
- Drag across the top to scroll through time; hold near the bottom to pin a vertical line to the
  nearest measurement and read its numbers and timestamp.

### Desktop (review)
- The trend chart is the primary element, not an afterthought.
- Raw daily averages and a smoothed moving average are both drawn, so noise is visible
  without hiding the signal.
- The history list is always reachable without navigating away from the chart.

## One palette, both apps

Both apps are dark only and share one palette. It is written twice, because the two frameworks
cannot read each other's resources:

- `src/BPTracker.Mobile/Resources/Styles/Colors.xaml`
- `src/BPTracker.Desktop/Themes/Theme.xaml`

Change one, change the other. Category names come from `BloodPressureCategoryName`, so at least
the words are shared in code.

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
