# 60 - Build and release

## Local

```powershell
./dev.ps1 setup      # restore tools + packages, report Android toolchain status
./dev.ps1 build      # build with warnings as errors
./dev.ps1 test       # run tests
./dev.ps1 coverage   # tests + HTML report + 85% gate, opens the report
./dev.ps1 gates      # everything CI enforces
./dev.ps1 desktop    # run the WPF app
./dev.ps1 emulator   # create the AVD if needed and boot the Android emulator
./dev.ps1 android    # build, install and launch on the emulator or a phone
./dev.ps1 devices    # list attached devices
```

VS Code tasks mirror all of these; `Ctrl+Shift+B` runs the build. `launch.json` debugs the WPF app.

## CI

[ci.yml](../.github/workflows/ci.yml) runs on every pull request and every push to `main`:
install the Android workload, build **both apps** with `-warnaserror`, run tests with coverage,
then fail if line coverage drops below 85%. The Android head is in the solution deliberately, so a
change that breaks the phone app fails on the pull request rather than at release time.

## Local Android toolchain

Building the phone app needs three things beyond the .NET SDK:

1. `dotnet workload install maui-android` (needs an elevated shell).
2. A JDK 17. `dev.ps1` looks for one under `%LOCALAPPDATA%\Programs\Microsoft\jdk-*`.
3. The Android SDK, provisioned with:
   `dotnet build src/BPTracker.Mobile/BPTracker.Mobile.csproj -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=True`

To run the phone app without a phone, add the emulator and a system image:

```
%LOCALAPPDATA%\Android\Sdk\cmdline-tools\latest\bin\sdkmanager.bat --install emulator "system-images;android-36;google_apis;x86_64"
```

Then `./dev.ps1 emulator` creates the `BPTracker_Pixel` AVD and boots it, and `./dev.ps1 android`
deploys to it. `avdmanager` exits non-zero over a harmless cmdline-tools location warning, so the
script checks the resulting AVD list rather than the exit code.

`./dev.ps1 setup` reports which of these are missing.

## Release

Every push to `main` publishes. [release.yml](../.github/workflows/release.yml) decides **which**
app is released from the paths that changed:

| Changed paths | Released |
| --- | --- |
| `src/BPTracker.{Domain,Application,Infrastructure,Presentation}/`, `build/`, `Directory.*`, `global.json`, `.config/`, `BPTracker.slnx` | both apps |
| `src/BPTracker.Desktop/` only | desktop only |
| `src/BPTracker.Mobile/` only | android only |
| docs, memory bank, workflows | neither |

If the base commit cannot be determined (first push, or rewritten history) it releases both,
which is the safe direction.

`workflow_dispatch` can force either app.

## Versioning

Two independent streams, each derived from its own tag prefix:

- Desktop: `desktop-v<major>.<minor>.<patch>`
- Android: `android-v<major>.<minor>.<patch>`

The release job reads the highest existing tag for its prefix and bumps the patch. With no tags
it starts at `0.1.0`. To bump major or minor, push a tag by hand
(for example `desktop-v1.0.0`) and the next automatic release continues from there.

The Android `versionCode` uses `github.run_number`, which is monotonic and never reused.

## Desktop updates

Velopack. `VelopackApp.Build().Run()` is the first statement in `Program.Main`, before any window
exists - it must stay there, because Velopack services install and update hooks at that point.

`vpk upload github` performs the upload so the `RELEASES` manifest the in-app updater reads always
matches the attached assets. Do not attach Velopack assets with `gh release upload` instead.

The update feed URL lives in `BPTracker.Desktop.csproj` as `AssemblyMetadata/ReleasesUrl` and is
read at runtime, so it is configured in exactly one place.

Per-device settings are stored under `LocalApplicationData`, **not** next to the executable,
because Velopack replaces the application directory on update. Readings live in the user's chosen
data folder, which is outside the install directory by design.

## Android signing

Set these repository secrets. Without `ANDROID_KEYSTORE_BASE64` the workflow builds an unsigned
APK, warns, and skips publishing, so the repository still works before signing is configured.

| Secret | Contents |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | The `.keystore` file, base64 encoded |
| `ANDROID_KEYSTORE_PASSWORD` | Keystore password |
| `ANDROID_KEY_ALIAS` | Key alias |
| `ANDROID_KEY_PASSWORD` | Key password |

The keystore is written to a temp path and deleted in an `always()` step. Never commit a
`.keystore` or `.jks`; `.gitignore` blocks both.
