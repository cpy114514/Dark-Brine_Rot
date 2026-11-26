param(
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Run,
    [ValidateRange(3,30)][int]$Seconds = 6,
    [switch]$Final
)

. (Join-Path $PSScriptRoot 'PerformancePlayer.ps1')

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance025'))
$launch = @{
    BuildRoot = $root
    RunRoot = Join-Path $root "runs/$Run"
    ExeName = 'DarkBrineQA025.exe'
    TimeoutSeconds = 240
    PlayerArguments = @(
        "-baseline-seconds=$Seconds", '-baseline-native1080', '-baseline-forest-attribution', '-baseline-forest-profiler'
    )
    Condition = [pscustomobject]@{ forestAttribution = $true; native1080 = $true; timingBuild = $true }
    NewRun = $true
}
Invoke-PerformancePlayer @launch