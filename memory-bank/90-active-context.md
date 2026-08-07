# 90 - Active context

Update this when you finish a piece of work. Keep it short; it is a status board, not a changelog.

## Current state

Both apps are built, green, and share one core. Quality gates, CI and release automation are in
place and verified.

- 4 shared libraries + WPF desktop + MAUI Android head, building with **zero warnings**.
- **205 tests**, line coverage **97.7%** against a required floor of 85%.
- `dev.ps1` runs every gate locally; VS Code tasks and a debug profile are wired up.
- CI builds both apps and publishes on every push to `main`, scoped by changed paths.

## Verified, not assumed

- The coverage gate genuinely fails: ReportGenerator exits 1 when the floor is not met.
  (It needs `--minimumCoverageThresholds`; a single dash is silently ignored.)
- The file-length gate genuinely fails: a 421 line file produced `error BP0001`.
- The desktop app launches, shows its window, and creates `Documents\BPTracker`.
- The Android app runs on an emulator: entry screen renders, Save writes the journal, and a
  restart seeds the fields from the last reading.
- Storage round-trips, merges journals from another device, and survives a truncated file.

## Open items

| Item | Notes |
| --- | --- |
| **Android on real hardware** | Verified on an Android 36 emulator, not on a physical phone. The all-files permission flow in particular behaves differently per vendor. |
| **Journal compaction** | Journals only grow. Irrelevant for years at a few readings a day, but see [ADR-0002](decisions/ADR-0002-journal-storage.md). |
| **Android signing secrets** | Not configured. Until they are, the workflow builds an unsigned APK and skips publishing. |
| **First release tags** | None yet, so the first release of each app will be `0.1.0`. |
| **Desktop history/trend on phone** | The phone is entry-only by design. Revisit only if asked. |

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
