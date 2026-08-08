# 30 - Tech stack

Versions are pinned centrally in [Directory.Packages.props](../Directory.Packages.props).
Never put a version number in an individual `.csproj`.

| Concern | Choice | Why this one |
| --- | --- | --- |
| SDK | .NET 10 (`global.json` pins 10.0.302) | Single toolchain for both apps. |
| Desktop UI | WPF (`net10.0-windows`) | Requested. Mature, good data grid and charting options. |
| Phone UI | .NET MAUI (`net10.0-android`) | Shares the C# core with WPF; one language, one test framework, one coverage report. Native Kotlin was rejected because it would duplicate every domain rule. |
| MVVM | CommunityToolkit.Mvvm | Source-generated observable properties and commands; works on both WPF and MAUI, so ViewModels are genuinely shared. |
| Storage | Append-only NDJSON files, no database | Sync is done by an external file-copying tool. SQLite was rejected because a whole-file copy of a live database corrupts it and loses data silently. See [ADR-0002](decisions/ADR-0002-journal-storage.md). |
| Desktop charts | LiveChartsCore SkiaSharp (WPF view) | Mature, and the desktop trend chart needs no custom drawing. |
| Phone chart | MAUI `GraphicsView` + `IDrawable` | Per-segment colouring at a healthy-band crossing means fighting a chart library's series model, and the geometry has to live in `Presentation` to stay covered. See [ADR-0003](decisions/ADR-0003-phone-chart.md). |
| Desktop updates | Velopack | Free, delta updates, first-class GitHub Releases support, no code-signing certificate required to get started. |
| Tests | xUnit v3 + Shouldly + NSubstitute | Shouldly gives readable failure messages. FluentAssertions was avoided because of its licence change. |
| Coverage | coverlet.collector + ReportGenerator | ReportGenerator can fail the build on a threshold, which is what makes the gate real. |
| Analyzers | .NET analyzers + SonarAnalyzer.CSharp + Roslynator | Sonar supplies the modularity rules (S107, S3776, S138) that enforce the SOLID requirements. |

## Local tools

Pinned in [.config/dotnet-tools.json](../.config/dotnet-tools.json); run `dotnet tool restore`.

- `reportgenerator` - coverage report and the 85% gate.
- `vpk` - Velopack packaging and upload.

## Prerequisites

Run `./dev.ps1 setup` on a fresh Windows machine. It installs the SDK version pinned in
`global.json` into the current user's local app data when necessary, restores the workloads the
solution requires (including `maui-android`), provisions the JDK and Android SDK, adds the
emulator and the pinned system image, then restores the repository's tools and packages. It is
safe to rerun.
