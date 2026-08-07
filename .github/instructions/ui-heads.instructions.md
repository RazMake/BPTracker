---
applyTo: "src/BPTracker.Desktop/**,src/BPTracker.Mobile/**"
---

# UI head rules

These projects are deliberately thin. See
[ADR-0001](../../memory-bank/decisions/ADR-0001-thin-heads-shared-core.md).

- **No business logic, validation or calculation.** It belongs in `BPTracker.Presentation` or
  deeper, where it is covered by the 85% gate. These projects are not measured.
- Code-behind may contain: `InitializeComponent`, event-to-command adaptation, framework type
  conversion, and navigation. Nothing else. An `if` on domain data is a smell.
- The composition root is the only place that names concrete implementations
  (`DesktopServices` / the MAUI `MauiProgram`).
- Chart and platform packages stay here. Never let `LiveChartsCore`, `System.Windows` or
  `Microsoft.Maui` types appear in shared projects.

## Traps that have already bitten

- `BPTracker.Application` shadows `System.Windows.Application` and
  `Microsoft.Maui.Controls.Application`. Fully qualify the framework type.
- On Android, `Android.OS.Environment` shadows `System.Environment`. Fully qualify.
- `IRelayCommand<int>` throws `ArgumentException` when XAML passes `CommandParameter="1"` as a
  string. Use a typed parameter:
  `<Button.CommandParameter><sys:Int32>1</sys:Int32></Button.CommandParameter>`.
- MAUI XAML source generation binds handlers as `EventHandler`, so code-behind handlers must take
  `object? sender`. `object sender` fails with CS8622.
- MAUI's `DisplayAlert` is obsolete; use `DisplayAlertAsync`.
- Anything guarded by an Android API level needs `OperatingSystem.IsAndroidVersionAtLeast(n)`,
  or CA1416 fails the build.
- **Every MAUI Essentials API needs its manifest permission.** `HapticFeedback` throws
  `PermissionException` without `android.permission.VIBRATE`, and it crashed the app *after* a
  successful save. Declare the permission, and never let a cosmetic effect throw on a path that
  has already committed data.
- Store per-device settings under `LocalApplicationData` (Windows) or `FileSystem.AppDataDirectory`
  (Android), never beside the executable and never in the synced data folder.
