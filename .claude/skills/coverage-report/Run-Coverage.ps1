<#
.SYNOPSIS
    Runs dotnet test with Coverlet coverage collection and emits the Cobertura XML path.

.PARAMETER SolutionPath
    Path to the .slnx or .sln file. Defaults to the first .slnx (then .sln) found at or one level below the repo root.

.PARAMETER OutputDir
    Directory for coverage output. Defaults to TestResults\coverage-analysis\raw at the repo root.

.PARAMETER Clean
    If set, deletes any previous raw output before running.

.OUTPUTS
    Writes COBERTURA_PATH:<path> to stdout. All other output goes to the host (not captured).
    Exit code mirrors dotnet test exit code (0 = all pass, 1 = test failures but coverage still valid).
#>
[CmdletBinding()]
param(
    [string] $SolutionPath,
    [string] $OutputDir,
    [switch] $Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = & git -C $PSScriptRoot rev-parse --show-toplevel

if (-not $SolutionPath) {
    $sln = Get-ChildItem -Path $repoRoot -Include "*.slnx", "*.sln" -Recurse -Depth 1 -ErrorAction SilentlyContinue |
        Sort-Object { $_.Extension -ne ".slnx" }, FullName |
        Select-Object -First 1
    if ($sln) { $SolutionPath = $sln.FullName }
}
if (-not $SolutionPath -or -not (Test-Path $SolutionPath)) {
    Write-Error "Solution not found: $SolutionPath"
    exit 2
}

if (-not $OutputDir) {
    # Join-Path in Windows PowerShell 5.1 only accepts two path segments at a
    # time (the multi-segment overload is PowerShell 7+ only) -- chain calls.
    $OutputDir = Join-Path $repoRoot "TestResults"
    $OutputDir = Join-Path $OutputDir "coverage-analysis"
    $OutputDir = Join-Path $OutputDir "raw"
}

if ($Clean -and (Test-Path $OutputDir)) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "Running: dotnet test $SolutionPath" -ForegroundColor Cyan
Write-Host "Output:  $OutputDir" -ForegroundColor Cyan

# coverlet.runsettings excludes source-generated *.g.cs (e.g. [LoggerMessage]
# output) so generator boilerplate does not dilute the authored-code number.
# Optional: a repo without the file still collects coverage, just unfiltered.
$runSettings = Join-Path $repoRoot "coverlet.runsettings"
$settingsArgs = @()
if (Test-Path $runSettings) {
    $settingsArgs = @("--settings", $runSettings)
} else {
    Write-Host "No coverlet.runsettings at repo root; generated files are included." -ForegroundColor Yellow
}

dotnet test $SolutionPath `
    --collect:"XPlat Code Coverage" `
    --results-directory $OutputDir `
    @settingsArgs `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include="[*]*" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[*.Tests]*,[*.Test]*,[*Tests]*,[*Test]*,[*.Specs]*,[*.Testing]*" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.SkipAutoProps=true

$testExitCode = $LASTEXITCODE

if ($testExitCode -gt 1) {
    Write-Error "dotnet test failed with exit code $testExitCode (build error)"
    exit $testExitCode
}

$xml = Get-ChildItem -Path $OutputDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $xml) {
    Write-Error "No coverage.cobertura.xml found under $OutputDir"
    exit 3
}

Write-Output "COBERTURA_PATH:$($xml.FullName)"
exit $testExitCode
