# Run from Windows PowerShell; uses only the established primary Debug build.
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'bin\Debug\net8.0-windows\Lazy App.exe'
$reportDirectory = Join-Path (Split-Path -Parent $executable) 'layout-check'

Push-Location -LiteralPath $projectRoot
try {
    if (-not $SkipBuild) {
        & dotnet build Lazy_App_Codex_Core.sln --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Primary build failed. No layout checks were launched.' }
    }
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Primary Debug executable was not found.' }

    $startedAt = [DateTime]::UtcNow
    $checkProcess = Start-Process -FilePath $executable -ArgumentList '--check-layout' -WindowStyle Hidden -Wait -PassThru
    $reportFile = Get-ChildItem -LiteralPath $reportDirectory -Filter 'report-*.json' -File |
        Where-Object { $_.LastWriteTimeUtc -ge $startedAt } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $reportFile) { throw "Layout checker exited with code $($checkProcess.ExitCode) without a new report." }

    $report = Get-Content -LiteralPath $reportFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    Write-Host "Report: $($reportFile.FullName)"
    Write-Host "Build SHA256: $($report.BuildSha256)"
    foreach ($case in $report.Cases) {
        $status = if ($case.Passed) { 'PASS' } else { 'FAIL' }
        Write-Host "$status $($case.Kind) $($case.Resolution) panels=$($case.Panels) nominalDpi=$($case.NominalDpi) actualDpi=$($case.EffectiveWindowDpi)"
        foreach ($check in $case.Checks | Where-Object { -not $_.Passed }) {
            Write-Host "  $($check.State): $($check.Name): $($check.Detail)"
        }
    }
    if ($report.Error) { Write-Host $report.Error }
    Write-Host 'Synthetic profiles are font/geometry stress checks, not native rendering on the other computer.'
    if (-not $report.Passed -or $checkProcess.ExitCode -ne 0) { exit 1 }
} finally {
    Pop-Location
}
