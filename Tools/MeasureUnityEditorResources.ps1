param(
    [Parameter(Mandatory=$true)][int]$EditorProcessId,
    [ValidateRange(5,600)][int]$DurationSeconds=180,
    [ValidateRange(1,30)][int]$IntervalSeconds=5
)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath (Split-Path $PSScriptRoot)).Path
$taskEvidence=Join-Path $taskWorkspace '.codex/editor-stability-20261102'
New-Item -ItemType Directory -Force $taskEvidence | Out-Null
$taskProcess=Get-Process -Id $EditorProcessId
if($taskProcess.Path -notlike '*\Editor\Unity.exe'){throw 'Target must be a Unity Editor process'}
$taskStarted=$taskProcess.StartTime
$taskOutput=Join-Path $taskEvidence ('resources-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'.jsonl')
$taskWatch=[Diagnostics.Stopwatch]::StartNew()
do {
    $taskProcess=Get-Process -Id $EditorProcessId -ErrorAction SilentlyContinue
    if(-not $taskProcess -or $taskProcess.StartTime -ne $taskStarted){
        [ordered]@{utc=[DateTime]::UtcNow.ToString('o');exited=$true;pid=$EditorProcessId} |
            ConvertTo-Json -Compress | Add-Content -LiteralPath $taskOutput
        throw 'The monitored editor exited'
    }
    [ordered]@{
        utc=[DateTime]::UtcNow.ToString('o');elapsedSeconds=[Math]::Round($taskWatch.Elapsed.TotalSeconds,2)
        pid=$EditorProcessId;handles=$taskProcess.HandleCount;threads=$taskProcess.Threads.Count
        workingSetBytes=$taskProcess.WorkingSet64;privateBytes=$taskProcess.PrivateMemorySize64
    } | ConvertTo-Json -Compress | Add-Content -LiteralPath $taskOutput
    if($taskWatch.Elapsed.TotalSeconds -lt $DurationSeconds){Start-Sleep -Seconds $IntervalSeconds}
} while($taskWatch.Elapsed.TotalSeconds -lt $DurationSeconds)
[pscustomobject]@{samples=$taskOutput;durationSeconds=[Math]::Round($taskWatch.Elapsed.TotalSeconds,1)} | ConvertTo-Json
