. (Join-Path $PSScriptRoot 'PerformancePlayer.ps1')

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance022'))
Invoke-PerformancePlayer -BuildRoot $root -RunRoot $root -ExeName 'DarkBrineQA022.exe' -TimeoutSeconds 420 -PlayerArguments @(
    '-baseline-seconds=6', '-baseline-native1080', '-baseline-all-styles', '-baseline-orbit'
)