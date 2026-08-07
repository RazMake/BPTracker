# ADR-0004 - Range is shown by shading the chart, not by colouring the line

- **Status:** accepted
- **Date:** 2026-08-07
- **Supersedes:** the per-segment line colouring decided in [ADR-0003](ADR-0003-phone-chart.md).
  Everything else in ADR-0003 still stands: the chart is hand-drawn, and its geometry lives in
  `BPTracker.Presentation`.

## Context

ADR-0003 coloured each line green or orange and split every run at the exact point it crossed a
healthy band edge. In use that is loud: on a screen with two lines, both changing colour several
times a day, the colour carries so much visual weight that the shape of the trend is hard to read.
It also cannot distinguish the two series from one another, because both use the same two colours.

## Decision

Both lines are drawn solid, in one colour each: systolic `#7EC8FF`, diastolic `#B79CFF`. Neither
is red, so red is free to mean exactly one thing.

The **background** is shaded pale green where each series is normal, and nowhere else.
`PressureBands.For` derives those two corridors from `HealthyRange`. Every band is **labelled**
with the series it belongs to, because the two series have different boundaries and a bare stripe
would be read as applying to both.

A **crisis is marked on the line, not the background**: `ChartLineBuilder` splits each run at the
point it crosses `CrisisThreshold` and paints the crossing part bright red. Shading the crisis
ranges was tried first and covered most of the chart, which left the shading meaning nothing.

A **tagged reading is a larger amber dot**, so the rare annotated reading is findable without
reading anything.

Both apps share this: the WPF chart maps the same `PressureBands` onto LiveCharts
`RectangularSection`s, overlays the crisis stretch as a second series, and marks tagged days with
a scatter series whose tooltip is the tag.

## Consequences

**Easy:**
- The trend shape is legible at a glance, the two series are told apart by hue, and red means
  one thing.
- Every rule lives in `Presentation` or `Domain`, so both renderers agree by construction.

**Hard:**
- The green bands are per series but the background is not, so a line passes through a band that
  is not about it. The labels are what keep this honest, and they are not optional decoration.
- LiveCharts paints a series in one colour, so the desktop's crisis stretch is a second series
  with nulls below the threshold. A lone crisis point between two normal ones draws as a dot with
  no line, where the phone draws the crossing exactly.
- `RectangularSection` in LiveCharts 2.0.5 has no label alignment, so the desktop's band label
  sits where the library puts it. It lands inside the band, which is what was wanted, but it is
  not under our control.
- The desktop chart plots daily averages, so its tag marker is per day and joins that day's tags.

**Revisit when:** a label is not enough and someone misreads a band as applying to the wrong
series. The fix would be per-series shading, drawn only around each line.

## Alternatives considered

- **Keep the coloured line and add shading.** Rejected: two encodings of the same fact, and the
  loudest one wins.
- **Shade every range, including the crisis ranges.** Tried, then rejected: almost the whole chart
  ends up tinted and the shading stops meaning anything.
- **Shade only behind each series' own corridor, clipped to that line's neighbourhood.** Rejected:
  the shading would move with the data and stop being a fixed reference to read against.
