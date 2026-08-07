# 90 - Active context

Update this when you finish a piece of work. Keep it short; it is a status board, not a changelog.

## Current state

Both apps are built, green, and share one core. Quality gates, CI and release automation are in
place and verified.

- 4 shared libraries + WPF desktop + MAUI Android head, building with **zero warnings**.
- **329 tests**, line coverage **98.4%** against a required floor of 85%.
- `dev.ps1` runs every gate locally; VS Code tasks and a debug profile are wired up.
- CI builds both apps and publishes on every push to `main`, scoped by changed paths.
- Both apps are dark only and share one palette and one chart style. See
  [ADR-0003](decisions/ADR-0003-phone-chart.md) and [ADR-0004](decisions/ADR-0004-chart-shading.md).

## Verified, not assumed

- The coverage gate genuinely fails: ReportGenerator exits 1 when the floor is not met.
  (It needs `--minimumCoverageThresholds`; a single dash is silently ignored.)
- The file-length gate genuinely fails: a 421 line file produced `error BP0001`.
- The desktop app launches, shows its window, and creates `Documents\BPTracker`.
- The Android app runs on an emulator: entry screen renders, Save writes the journal, and a
  restart seeds the fields from the last reading.
- Sliding a finger across a number changes it: a 300 px swipe moved systolic 120 -> 101, which
  confirms MAUI reports `PanUpdatedEventArgs.TotalX/TotalY` in device-independent units.
- The chart draws 42 seeded readings: lines interpolate between measurements, the colour changes
  at the healthy band edge rather than at a measurement, dragging the top scrolls, and holding the
  bottom pins a vertical line with "100 / 87 mmHg, Tue 4 Aug 11:34".
- Save disables itself after a save and re-enables as soon as a number changes, confirmed on the
  emulator through the accessibility tree.
- The desktop renders dark end to end, with the same shaded corridors and line colours as the
  phone, and spells categories out as "Hypertension stage 1".
- Both pages relayout on rotation: the entry screen goes from stacked to three columns.
- Storage round-trips, merges journals from another device, and survives a truncated file.

## Open items

| Item | Notes |
| --- | --- |
| **Android on real hardware** | Verified on an Android 36 emulator, not on a physical phone. The all-files permission flow in particular behaves differently per vendor. |
| **Journal compaction** | Journals only grow. Irrelevant for years at a few readings a day, but see [ADR-0002](decisions/ADR-0002-journal-storage.md). |
| **Android release** | Signing secrets are configured. The workflow preflights the restored keystore, alias and both passwords before publishing. Rerun it after correcting any secret named by that check. |
| **First release tags** | None yet, so the first release of each app will be `0.1.0`. |
| **Phone chart on a device** | Builds and is unit tested, but the touch bands and drag feel have not been tried on real hardware. |
| **Phone chart zoom** | The horizontal scale is fixed at `ChartRequest.DefaultPixelsPerHour`. Pinch-zoom is not implemented. |
| **Faint duplicate near the top of the entry screen** | On the Android 36 emulator a dim copy of the Save button's text is drawn about 680 dip above the button. `uiautomator dump` and `dumpsys activity top` both show exactly one button, so it is a rendering artifact, not a duplicated view. It survives a cold start, a FlexLayout rewrite and removing the Border's `StrokeShape`. Check on real hardware before spending more time on it. |
| **Arm and position** | Still on the reading and still written by the desktop; neither app asks for them. |

## Running the phone app locally

```powershell
./dev.ps1 emulator   # create the AVD if needed and boot it
./dev.ps1 android    # build, install and launch (boots the emulator if nothing is attached)
./dev.ps1 devices    # what is currently attached
```

The AVD is `BPTracker_Pixel` on `system-images;android-36;google_apis;x86_64`.

## Local toolchain

The Android build needs a JDK and the Android SDK. On this machine they are at:

- JDK: `%LOCALAPPDATA%\Programs\Microsoft\jdk-17.0.20+8`
- SDK: `%LOCALAPPDATA%\Android\Sdk`

`dev.ps1` finds the JDK automatically; `./dev.ps1 setup` reports what is missing.

## Watch out for

- `BPTracker.Application` (our namespace) shadows `System.Windows.Application` and
  `Microsoft.Maui.Controls.Application`. Fully qualify the framework type in UI heads.
- On Android, `Android.OS.Environment` shadows `System.Environment`. Fully qualify.
- `IRelayCommand<int>` throws `ArgumentException` if XAML passes a string. Use a typed
  `<sys:Int32>` command parameter.
- MAUI's template `Styles.xaml` is 434 lines and trips the file-length gate. It was trimmed to the
  controls the app actually uses.
- MAUI XAML source generation binds handlers as `EventHandler`, so code-behind handlers must take
  `object? sender`, not `object sender`.
- MAUI Essentials APIs need their manifest permission or they throw at runtime. `HapticFeedback`
  needs `android.permission.VIBRATE`; without it the app crashed *after* saving successfully.
- The MAUI XAML source generator rejects `BasedOn="{StaticResource {x:Type Button}}"`. A keyed
  style has to spell its setters out, because it replaces the implicit style outright anyway.
- Roslynator's `RCS1139` fails the build on a `<remarks>` doc comment with no `<summary>`. Use a
  plain `//` comment in the UI heads, which do not generate documentation.
- `CA1861` fails the build on an inline array argument inside a test. Hoist expected arrays to
  `private static readonly` fields.
- Reassigning `Grid.Row` / `Grid.Column` at runtime to relayout for orientation left the previous
  layout painted behind the new one on Android. The entry screen uses a `FlexLayout` and switches
  `Direction` instead, which is one property and reflows cleanly.
- A custom `IDrawable` must take **every** coordinate from one source. Mixing `dirtyRect` with the
  view model's plot size put the chart's date labels in a different place during a layout pass,
  and the stale labels stayed on screen.
- **PowerShell allows `?` in a variable name**, so `$candidate?.FullName` reads a variable called
  `candidate?` and silently yields `$null`. It needs `${candidate}?.FullName`, or an explicit
  `if`. This made `dev.ps1 android` report "No JDK found" on every machine where `JAVA_HOME` was
  not already set, while passing for anyone who had exported it.
- `$lines -notmatch 'x'` returns every line that does not match, so it is truthy whenever the
  output has more than one line. To test "no line matches", use `-not ($lines -match 'x')`.
  This made `dev.ps1 setup` claim the Android workload was missing when it was installed.
- An implicit WPF `TextBlock` style leaks into a `ComboBox`'s selected-item presenter and can make
  the text invisible. Scope a nested style inside `Style.Resources` on the `ComboBox`.
- LiveCharts draws on its own canvas, which stays white until the `CartesianChart` control's
  `Background` is set. The surrounding dark card does not reach it.
