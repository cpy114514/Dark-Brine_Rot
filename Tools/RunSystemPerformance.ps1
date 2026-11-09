param(
    [ValidateSet('before','before-v2','after','after-gpu','before-route','after-route','after-gpu-route','after-gpu-release')] [string] $Variant = 'before-v2',
    [string] $Run = 'throughput',
    [int] $Seconds = 8,
    [switch] $Profile,
    [switch] $Foreground
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/system-performance-20261107'))
$exe = Join-Path $root "$Variant/DarkBrineRot_PerformanceQA.exe"
$output = Join-Path $root "$Variant/$Run"
if (!(Test-Path -LiteralPath ($exe+'.build.json'))) { throw 'Build report is missing' }
if ((Get-Content -LiteralPath ($exe+'.build.json') -Raw | ConvertFrom-Json).result -ne 'Succeeded') { throw 'Build did not succeed' }
if (Test-Path -LiteralPath $output) { throw 'Use a new run label to preserve evidence' }
if (Get-Process DarkBrineRot_PerformanceQA -ErrorAction SilentlyContinue) { throw 'Another performance player is running' }
New-Item -ItemType Directory -Path $output | Out-Null
$report = Join-Path $output 'report.json'
$log = Join-Path $output 'player.log'
$arguments = @('-sahur-baseline','-baseline-native1080','-baseline-startup-render','-system-performance',"-baseline-seconds=$Seconds",
    ('-baseline-report="'+$report+'"'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$log+'"'))
if ($Profile) { $arguments += '-system-profile' }
$windowStyle = if ($Foreground) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle $windowStyle -PassThru
$process.Id | Set-Content (Join-Path $output 'owned.pid')
try {
    if (!$process.WaitForExit(240000)) { throw 'Performance player exceeded the test timeout' }
    if ($process.ExitCode -ne 0) { throw "Player exit code $($process.ExitCode)" }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $result | Select-Object status,width,height,quality,graphicsApi,errors,memoryMb,loadSeconds,skippedPhases
    if ($result.status -ne 'completed') { throw "Incomplete capture: $($result.status)" }
    python (Join-Path $PSScriptRoot 'SummarizeSystemPerformance.py') $output --output (Join-Path $output 'summary.json')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot summarize the capture' }
    $summary = Get-Content -LiteralPath (Join-Path $output 'summary.json') -Raw | ConvertFrom-Json
    $rows = foreach ($phase in $summary.before.phases.PSObject.Properties) {
        $phase.Value | Select-Object @{n='phase';e={$phase.Name}},frames,averageMs,p95Ms,onePercentLowFps
    }
    $rows | Format-Table
} finally {
    if (!$process.HasExited) { Stop-Process -InputObject $process }
    $process.Dispose()
}
