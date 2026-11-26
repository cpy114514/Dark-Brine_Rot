param(
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Run,
    [switch]$Early,
    [switch]$NoSync,
    [switch]$Adaptive,
    [switch]$Fixed
)

. (Join-Path $PSScriptRoot 'PerformancePlayer.ps1')

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance023'))
$buildRoot = if ($Adaptive) { Join-Path $root 'adaptive-build' } else { $root }
$playerArguments = @('-baseline-seconds=3', '-baseline-load-trace', '-baseline-load-only')
if ($Early) { $playerArguments += '-baseline-startup-render' }
if ($Fixed) { $playerArguments += '-baseline-fixed-init-budget' }
$condition = [pscustomobject]@{
    earlyRender = [bool]$Early
    temporaryVsyncOff = [bool]$NoSync
    adaptiveBuild = [bool]$Adaptive
    fixedBudget = [bool]$Fixed
}

$launch = @{
    BuildRoot = $buildRoot
    RunRoot = Join-Path $root "runs/$Run"
    ExeName = 'DarkBrineQA023.exe'
    TimeoutSeconds = 240
    PlayerArguments = $playerArguments
    Condition = $condition
    NewRun = $true
    NoSync = $NoSync
}
Invoke-PerformancePlayer @launch