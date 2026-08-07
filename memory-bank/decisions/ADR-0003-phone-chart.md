# ADR-0003 - The phone chart is hand-drawn, and its geometry lives in Presentation

- **Status:** accepted, with the line colouring superseded by
  [ADR-0004](ADR-0004-chart-shading.md). Range is now shown by shading the background; both lines
  are solid. The rest of this record still holds.
- **Date:** 2026-08-06
- **Amends:** [10-product.md](../10-product.md), which described the phone as entry-only

## Context

The phone needed a chart of every measurement: two series, proportional time on the x axis, the
line coloured by whether it is inside the healthy band, scrolled by dragging and inspected by
holding a finger against it. The colour has to change **where the line crosses the band**, not at
the nearest measurement, or a run that spends most of its length out of range reads as healthy.

Two constraints pull against each other:

- [ADR-0001](ADR-0001-thin-heads-shared-core.md) says logic lives in the shared libraries, and the
  85% coverage gate is only honest because the UI heads contain none.
- A chart is mostly geometry, which is exactly the kind of logic that tends to end up in a view.

`LiveChartsCore.SkiaSharpView.Maui` was already pinned in `Directory.Packages.props` and unused.

## Decision

The chart is drawn with MAUI's built-in `GraphicsView` and an `IDrawable`, and **all** of its
geometry lives in `BPTracker.Presentation.Charts`.

`ChartViewModel` produces an immutable `ChartFrame` holding finished pixel coordinates, coloured
`ChartSegment`s, grid lines, date ticks and the read-out strings. `ChartDrawable` iterates that and
calls `DrawLine` / `DrawString`. There is no arithmetic, no threshold and no formatting in the
mobile head.

Touch is split by position rather than by mode: `ChartTouch` maps the top 55% of the plot to
scrolling and the rest to inspecting, so one finger does both without a toggle.

The band edges themselves are a domain rule, held once in `HealthyRange` and consumed by both
`BloodPressureClassifier` and the chart, so a category and a colour can never disagree.

## Consequences

**Easy:**
- Every chart behaviour is unit tested with no emulator: segment splitting at band crossings,
  scroll clamping, tick spacing, cursor snapping. Coverage stayed above 98%.
- No new package reference, and nothing platform-specific leaked into a shared project.
- The desktop keeps LiveCharts; the two heads are free to draw differently because they share the
  data, not the renderer.

**Hard:**
- Panning, zooming, animation and accessibility are ours to write. Only panning exists today;
  the zoom is a fixed `ChartRequest.DefaultPixelsPerHour`.
- `ChartFrame` is rebuilt on every invalidate. Fine for hundreds of readings, wrong for tens of
  thousands.

**Revisit when:** pinch-zoom, animated transitions or screen-reader support are wanted, or the
frame build shows up in a profile.

## Alternatives considered

- **LiveCharts on the phone**, as on the desktop. Rejected: per-segment colouring at a threshold
  crossing means fighting the library's series model, the geometry would move into the head or into
  library-specific types, and it pulls SkiaSharp into the Android head for one screen.
- **Colour each segment by its starting measurement.** Rejected: a line from 110 to 190 would be
  drawn entirely green.
- **A separate scroll gesture mode with a toggle button.** Rejected: the phone screen is the thing
  in short supply, and a mode is a thing to remember.
