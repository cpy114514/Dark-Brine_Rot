param(
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Run,
    [ValidateRange(3,30)][int]$Seconds = 6,
    [switch]$Final
)

. (Join-Path $PSScriptRoot 'PerformancePlayer.ps1')

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance024'))
$launch = @{
    BuildRoot = if ($Final) { Join-Path $root 'final-build' } else { $root }
    RunRoot = Join-Path $root "runs/$Run"
    ExeName = 'DarkBrineQA024.exe'
    TimeoutSeconds = 240
    PlayerArguments = @("-baseline-seconds=$Seconds", '-baseline-native1080', '-baseline-forest-attribution')
    Condition = [pscustomobject]@{ forestAttribution = $true; native1080 = $true; timingBuild = $true }
    NewRun = $true
}
Invoke-PerformancePlayer @launch