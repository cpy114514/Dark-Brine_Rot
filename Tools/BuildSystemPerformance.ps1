param([ValidateSet('before-v2', 'after', 'after-gpu', 'before-route', 'after-route', 'after-gpu-route', 'after-gpu-release')] [string] $Variant)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root = Join-Path $project '.codex/system-performance-20261107'
$clone = Join-Path $root 'build-workspace'
$snapshot = Join-Path $root 'snapshot-ready.txt'
if (!(Test-Path -LiteralPath $snapshot) -or (Get-Content -LiteralPath $snapshot -Raw).Trim() -ne $clone) {
    throw 'A verified project snapshot is required.'
}
$output = Join-Path $root "$Variant/DarkBrineRot_PerformanceQA.exe"
if (Test-Path -LiteralPath ($output+'.build.json')) { throw 'Build label already exists' }
$existing = Get-CimInstance Win32_Process -Filter "name='Unity.exe'"
if ($existing | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($clone) }) {
    throw 'The isolated snapshot Editor is still running.'
}
$hostEditor = $existing | Where-Object { $_.CommandLine -match '-useHub' -and $_.CommandLine.Contains($project) } | Select-Object -First 1
if (!$hostEditor) { throw 'Cannot resolve the pinned Unity Editor' }
$performanceFiles = @(
    'Assets/Ocean/OceanWorld.cs',
    'Assets/Scripts/Performance/NaturalGrassScatter.cs',
    'Assets/Scripts/Performance/WorldPerformanceManager.cs'
)
$deathFiles = @(
    'Assets/Scripts/Enemy/EnemyDeathPresentation.cs',
    'Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Scripts/CappuccinoMotionRig.cs',
    'Assets/Game/Prefabs/UI/EquipmentPickup.cs'
)
foreach ($file in $performanceFiles + $deathFiles) {
    $source = Join-Path $project $file
    if ($Variant.StartsWith('before')) {
        $backup = if ($file -in $performanceFiles) {
            Join-Path $root 'source-before'
        } else {
            Join-Path $project '.codex/boss-death-polish-20261108/source-before'
        }
        $source = Join-Path $backup ($file.Replace('/', '_'))
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $clone $file)
}
$env:BRINE_PERFORMANCE_GPU_SKINNING = if ($Variant.StartsWith('after-gpu')) { '1' } else { '0' }
$env:BRINE_PERFORMANCE_RELEASE = if ($Variant.EndsWith('-release')) { '1' } else { '0' }
if ($Variant.EndsWith('-route')) {
    $baseline = Join-Path $clone 'Assets/Scripts/Performance/IslandPerformanceBaseline.cs'
    $source = Get-Content -LiteralPath $baseline -Raw
    $offset = 'Vector3 offset = new Vector3(0, 5, -11);'
    $heading = 'if (systemMeasuring) offset = Quaternion.LookRotation(systemHeading) * offset;'
    if (!$source.Contains('CollectSystemFrame(wallMs);') -or !$source.Contains($offset)) {
        throw 'Missing the system route hooks in the isolated baseline.'
    }
    if (!$source.Contains($heading)) {
        $source.Replace($offset, "$offset`n                $heading") | Set-Content -LiteralPath $baseline -NoNewline
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SystemPerformanceRoutes.cs') -Destination (Join-Path $clone 'Assets/Scripts/Performance/SystemPerformanceRoutes.cs')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SystemPerformanceBuild.cs') -Destination (Join-Path $clone 'Assets/Editor/SystemPerformanceBuild.cs')
$env:BRINE_PERFORMANCE_BUILD = $output
$log = Join-Path $root "build-$Variant.log"
$arguments = @('-batchmode','-quit','-nographics','-projectPath',('"' + $clone + '"'),
    '-executeMethod','SystemPerformanceBuild.Run','-logFile',('"' + $log + '"'))
$process = Start-Process -FilePath $hostEditor.ExecutablePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.Id | Set-Content -LiteralPath (Join-Path $root "build-$Variant.pid")
$report = $output + '.build.json'
try {
    $deadline = [DateTime]::UtcNow.AddMinutes(15)
    while (!(Test-Path -LiteralPath $report) -and !$process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Seconds 2
    }
    if (!(Test-Path -LiteralPath $report)) { throw "Build report unavailable. See $log" }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $result | Select-Object result,seconds,totalErrors,totalWarnings,totalSize
    if ($result.result -ne 'Succeeded') { throw "Build failed. See $log" }
    if (!$process.WaitForExit(20000)) {
        # Unity has already written the final report. Only the owned build process
        # is stopped if its isolated shutdown stalls.
        Stop-Process -InputObject $process
    }
} finally {
    $process.Dispose()
}
