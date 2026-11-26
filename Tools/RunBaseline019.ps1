. (Join-Path $PSScriptRoot 'PerformancePlayer.ps1')

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance019'))
Invoke-PerformancePlayer -BuildRoot $root -RunRoot $root -ExeName 'DarkBrineQA.exe' -TimeoutSeconds 260 -PlayerArguments @(
    '-baseline-seconds=10'
)