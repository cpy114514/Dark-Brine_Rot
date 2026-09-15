# Read-only repository audit. No Unity process or asset writes are needed.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$assetRoot = Join-Path $projectRoot 'Assets'
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'Run this script from the Tools folder of a Unity project.'
}
$files = @(Get-ChildItem -LiteralPath $assetRoot -Recurse -File)
$assets = @($files | Where-Object Extension -ne '.meta')
$missingMeta = @($assets | Where-Object { -not (Test-Path -LiteralPath ($_.FullName + '.meta')) })
$metadata = @($files | Where-Object Extension -eq '.meta')
$guidRecords = @($metadata | ForEach-Object {
    $match = Select-String -LiteralPath $_.FullName -Pattern '^guid: ([0-9a-f]{32})$'
    if ($match) { [pscustomobject]@{ Guid = $match.Matches[0].Groups[1].Value; Path = $_.FullName } }
})
$duplicateGuids = @($guidRecords | Group-Object Guid | Where-Object Count -gt 1)
$fbxHashes = @($assets | Where-Object Extension -eq '.fbx' | ForEach-Object {
    [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($projectRoot, $_.FullName); Bytes = $_.Length; Hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$duplicateModels = @($fbxHashes | Group-Object Hash | Where-Object Count -gt 1)
[pscustomobject]@{
    AssetFiles = $assets.Count
    AssetSizeMB = [math]::Round(($assets | Measure-Object Length -Sum).Sum / 1MB, 2)
    MissingMetaFiles = $missingMeta.Count
    DuplicateGuidGroups = $duplicateGuids.Count
    IdenticalFbxGroups = $duplicateModels.Count
} | Format-List
foreach ($file in $missingMeta) { Write-Warning "Missing .meta: $($file.FullName)" }
foreach ($group in $duplicateGuids) { Write-Warning "Duplicate GUID: $($group.Group.Path -join ', ')" }
foreach ($group in $duplicateModels) {
    Write-Output 'Identical FBX content (import settings and references may differ):'
    $group.Group | Select-Object Path,Bytes | Format-Table -AutoSize
}
if ($missingMeta.Count -gt 0 -or $duplicateGuids.Count -gt 0) { exit 1 }
