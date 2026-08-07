#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Single entry point for everyday development tasks.

.DESCRIPTION
    Every quality gate that CI enforces is runnable here, so a developer never
    discovers a gate failure for the first time in a pull request.

.EXAMPLE
    ./dev.ps1 coverage
    Runs the tests, builds an HTML coverage report, enforces the 85% floor and opens the report.

.EXAMPLE
    ./dev.ps1 desktop
    Builds and launches the WPF app.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('build', 'test', 'coverage', 'gates', 'desktop', 'android', 'emulator', 'devices', 'format', 'clean', 'setup')]
    [string]$Task = 'gates',

    # Skip opening the coverage report in a browser.
    [switch]$NoBrowser,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$solution = Join-Path $repoRoot 'BPTracker.slnx'
$artifacts = Join-Path $repoRoot 'artifacts'
$coverageDir = Join-Path $artifacts 'coverage'
$reportDir = Join-Path $artifacts 'coveragereport'
$runSettings = Join-Path $repoRoot 'build/coverage.runsettings'

$androidSdk = Join-Path $env:LOCALAPPDATA 'Android/Sdk'
$avdName = 'BPTracker_Pixel'
$systemImage = 'system-images;android-36;google_apis;x86_64'

# Keep this in step with the threshold in .github/workflows/ci.yml.
$coverageThreshold = 85

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Checked {
    param([scriptblock]$Command, [string]$What)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$What failed with exit code $LASTEXITCODE."
    }
}

function Invoke-Build {
    Write-Step "Building ($Configuration), warnings are errors"
    Invoke-Checked { dotnet build $solution -c $Configuration --nologo -warnaserror } 'Build'
}

function Invoke-Test {
    Write-Step 'Running tests'
    Invoke-Checked { dotnet test $solution -c $Configuration --nologo } 'Tests'
}

function Invoke-Coverage {
    Write-Step 'Running tests with coverage'

    if (Test-Path $coverageDir) { Remove-Item $coverageDir -Recurse -Force }
    if (Test-Path $reportDir) { Remove-Item $reportDir -Recurse -Force }

    Invoke-Checked {
        dotnet test $solution -c $Configuration --nologo `
            --collect 'XPlat Code Coverage' `
            --settings $runSettings `
            --results-directory $coverageDir
    } 'Tests'

    Write-Step "Building report and enforcing the $coverageThreshold% floor"
    Invoke-Checked { dotnet tool restore } 'Tool restore'

    # ReportGenerator exits non-zero when line coverage falls under the threshold,
    # which is what actually enforces the gate. The threshold is a *setting*, so it needs
    # the double-dash prefix; with a single dash it is silently ignored.
    Invoke-Checked {
        dotnet reportgenerator `
            "-reports:$coverageDir/**/coverage.cobertura.xml" `
            "-targetdir:$reportDir" `
            '-reporttypes:Html;MarkdownSummaryGithub;TextSummary' `
            "--minimumCoverageThresholds:lineCoverage=$coverageThreshold"
    } 'Coverage gate'

    $summary = Join-Path $reportDir 'Summary.txt'
    if (Test-Path $summary) {
        Write-Host ''
        Get-Content $summary | Select-Object -First 20 | Write-Host
    }

    $index = Join-Path $reportDir 'index.html'
    if (-not $NoBrowser -and (Test-Path $index)) {
        Write-Step 'Opening coverage report'
        Start-Process $index
    }
}

function Invoke-Desktop {
    $project = Join-Path $repoRoot 'src/BPTracker.Desktop/BPTracker.Desktop.csproj'
    if (-not (Test-Path $project)) {
        throw 'The desktop project does not exist yet.'
    }

    Write-Step 'Launching the desktop app'
    Invoke-Checked { dotnet run --project $project -c $Configuration } 'Desktop run'
}

function Get-AttachedDevices {
    $adb = Join-Path $androidSdk 'platform-tools/adb.exe'
    if (-not (Test-Path $adb)) {
        return @()
    }

    return & $adb devices |
        Select-Object -Skip 1 |
        Where-Object { $_ -match '\sdevice$' } |
        ForEach-Object { ($_ -split '\s+')[0] }
}

function Invoke-Devices {
    Write-Step 'Attached Android devices'
    $devices = Get-AttachedDevices
    if ($devices.Count -eq 0) {
        Write-Host 'None. Run ./dev.ps1 emulator, or plug in a phone with USB debugging on.' -ForegroundColor Yellow
    }
    else {
        $devices | ForEach-Object { Write-Host "  $_" -ForegroundColor Green }
    }
}

function Start-Emulator {
    $emulator = Join-Path $androidSdk 'emulator/emulator.exe'
    if (-not (Test-Path $emulator)) {
        throw "Emulator not installed. Run: $androidSdk\cmdline-tools\latest\bin\sdkmanager.bat --install emulator `"$systemImage`""
    }

    if ((& $emulator -list-avds) -notcontains $avdName) {
        Write-Step "Creating the $avdName virtual device"
        $avdManager = Join-Path $androidSdk 'cmdline-tools/latest/bin/avdmanager.bat'
        'no' | & $avdManager create avd --name $avdName --package $systemImage --device 'pixel_6' --force

        # avdmanager exits non-zero over a harmless cmdline-tools location warning,
        # so trust the resulting list rather than the exit code.
        if ((& $emulator -list-avds) -notcontains $avdName) {
            throw 'Could not create the virtual device.'
        }
    }

    Write-Step "Booting $avdName"
    Start-Process -FilePath $emulator -ArgumentList @('-avd', $avdName, '-netdelay', 'none', '-netspeed', 'full')

    $adb = Join-Path $androidSdk 'platform-tools/adb.exe'
    Write-Host 'Waiting for the emulator to finish booting...' -ForegroundColor DarkGray
    & $adb wait-for-device

    # wait-for-device returns as soon as adb connects, which is well before Android is usable.
    for ($i = 0; $i -lt 120; $i++) {
        if ((& $adb shell getprop sys.boot_completed 2>$null) -match '1') {
            Write-Host 'Emulator ready.' -ForegroundColor Green
            return
        }
        Start-Sleep -Seconds 2
    }

    throw 'The emulator did not finish booting in time.'
}

function Resolve-JavaHome {
    if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME 'bin/java.exe'))) {
        return $env:JAVA_HOME
    }

    # The Android workload needs a JDK but does not set JAVA_HOME for us.
    $candidate = Get-ChildItem "$env:LOCALAPPDATA/Programs/Microsoft" -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'jdk-*' } |
        Sort-Object Name -Descending |
        Select-Object -First 1

    return $candidate?.FullName
}

function Invoke-Android {
    $project = Join-Path $repoRoot 'src/BPTracker.Mobile/BPTracker.Mobile.csproj'
    if (-not (Test-Path $project)) {
        throw 'The mobile project does not exist yet.'
    }

    $javaHome = Resolve-JavaHome
    if (-not $javaHome) {
        throw 'No JDK found. Run ./dev.ps1 setup for instructions.'
    }
    $env:JAVA_HOME = $javaHome

    if ((Get-AttachedDevices).Count -eq 0) {
        Write-Host 'No device attached; starting the emulator.' -ForegroundColor Yellow
        Start-Emulator
    }

    Write-Step 'Deploying to the Android device'
    Invoke-Checked {
        dotnet build $project -c $Configuration -f net10.0-android -t:Run
    } 'Android deploy'
}

function Invoke-Format {
    Write-Step 'Applying code style'
    Invoke-Checked { dotnet format $solution } 'Format'
}

function Invoke-Clean {
    Write-Step 'Cleaning build output'
    foreach ($dir in @($artifacts)) {
        if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
    }
    Invoke-Checked { dotnet clean $solution --nologo } 'Clean'
}

function Invoke-Setup {
    Write-Step 'Restoring tools and packages'
    Invoke-Checked { dotnet tool restore } 'Tool restore'
    Invoke-Checked { dotnet restore $solution } 'Restore'

    Write-Step 'Checking the Android toolchain'

    $workloads = dotnet workload list
    if ($workloads -notmatch 'android') {
        Write-Host 'Android workload missing. In an elevated shell run:' -ForegroundColor Yellow
        Write-Host '    dotnet workload install maui-android' -ForegroundColor Yellow
    }
    else {
        Write-Host 'Android workload present.' -ForegroundColor Green
    }

    $javaHome = Resolve-JavaHome
    if ($javaHome) {
        Write-Host "JDK found: $javaHome" -ForegroundColor Green
        $env:JAVA_HOME = $javaHome
    }
    else {
        Write-Host 'No JDK found. Download Microsoft OpenJDK 17 and extract it to' -ForegroundColor Yellow
        Write-Host "    $env:LOCALAPPDATA\Programs\Microsoft" -ForegroundColor Yellow
        Write-Host '    https://aka.ms/download-jdk/microsoft-jdk-17-windows-x64.zip' -ForegroundColor Yellow
    }

    if (Test-Path "$env:LOCALAPPDATA/Android/Sdk/platforms") {
        Write-Host 'Android SDK present.' -ForegroundColor Green
    }
    else {
        Write-Host 'Android SDK missing. With JAVA_HOME set, run:' -ForegroundColor Yellow
        Write-Host '    dotnet build src/BPTracker.Mobile/BPTracker.Mobile.csproj -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=True' -ForegroundColor Yellow
    }
}

Push-Location $repoRoot
try {
    # The solution includes the Android head, so any build needs a JDK on hand.
    $resolvedJava = Resolve-JavaHome
    if ($resolvedJava) {
        $env:JAVA_HOME = $resolvedJava
    }

    switch ($Task) {
        'build' { Invoke-Build }
        'test' { Invoke-Test }
        'coverage' { Invoke-Coverage }
        'desktop' { Invoke-Desktop }
        'android' { Invoke-Android }
        'emulator' { Start-Emulator }
        'devices' { Invoke-Devices }
        'format' { Invoke-Format }
        'clean' { Invoke-Clean }
        'setup' { Invoke-Setup }
        'gates' {
            Invoke-Build
            Invoke-Coverage
            Write-Host ''
            Write-Host 'All gates passed.' -ForegroundColor Green
        }
    }
}
finally {
    Pop-Location
}
