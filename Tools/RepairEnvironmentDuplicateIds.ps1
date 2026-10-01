$ErrorActionPreference = 'Stop'
$sceneRepairPath = [System.IO.Path]::GetFullPath('Assets/Scenes/First Island/Environment.unity')
if (Get-Process -Name Unity -ErrorAction SilentlyContinue | Where-Object { $_.Id -eq 24300 }) {
    throw 'The project Editor is still running; do not rewrite a live scene.'
}
$sceneRepairText = [System.IO.File]::ReadAllText($sceneRepairPath)
$sceneRepairMatches = [regex]::Matches($sceneRepairText, '(?ms)^--- !u!\d+ &(?<id>\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)')
$sceneRepairGroups = @($sceneRepairMatches | Group-Object { $_.Groups['id'].Value } | Where-Object Count -gt 1)
$sceneRepairAllowed = @('1125180043','1170661985','1170661986','1170661987','1170661988')
if ($sceneRepairGroups.Count -ne 5) { throw 'Unexpected duplicate set.' }
foreach ($sceneRepairGroup in $sceneRepairGroups) {
    if ($sceneRepairGroup.Name -notin $sceneRepairAllowed -or $sceneRepairGroup.Count -ne 3) { throw 'Unexpected duplicate identifier.' }
}
if ($sceneRepairText -match 'fileID: 1125180043[},]') { throw 'Duplicate breaker mesh is referenced; needs additional review.' }
# The duplicated foam MeshFilters differ only in a regenerated mesh reference.
$sceneRepairMeshFilters = @($sceneRepairGroups | Where-Object Name -eq '1170661988' | ForEach-Object { $_.Group } | ForEach-Object { $_.Value })
$sceneRepairNormalizedFilters = @($sceneRepairMeshFilters | ForEach-Object { [regex]::Replace($_, 'm_Mesh: \{fileID: \d+\}', 'm_Mesh: {fileID: GENERATED}') } | Sort-Object -Unique)
if ($sceneRepairNormalizedFilters.Count -ne 1) { throw 'Foam MeshFilters differ beyond their generated mesh.' }
foreach ($sceneRepairId in '1170661985','1170661986','1170661987') {
    $sceneRepairUnique = @($sceneRepairGroups | Where-Object Name -eq $sceneRepairId | ForEach-Object { $_.Group } | ForEach-Object { $_.Value } | Sort-Object -Unique)
    if ($sceneRepairUnique.Count -ne 1) { throw "Non-identical foam object $sceneRepairId." }
}
$sceneRepairBackupDir = [System.IO.Path]::GetFullPath('Tools/Backups')
[System.IO.Directory]::CreateDirectory($sceneRepairBackupDir) | Out-Null
$sceneRepairBackup = [System.IO.Path]::Combine($sceneRepairBackupDir, 'Environment.before-duplicate-repair.' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '.unity.txt')
[System.IO.File]::Copy($sceneRepairPath, $sceneRepairBackup, $false)
$sceneRepairSeen = [System.Collections.Generic.HashSet[string]]::new()
$sceneRepairBuilder = [System.Text.StringBuilder]::new()
[void]$sceneRepairBuilder.Append($sceneRepairText.Substring(0, $sceneRepairMatches[0].Index))
$sceneRepairRemoved = 0
foreach ($sceneRepairMatch in $sceneRepairMatches) {
    if ($sceneRepairSeen.Add($sceneRepairMatch.Groups['id'].Value)) { [void]$sceneRepairBuilder.Append($sceneRepairMatch.Value) }
    else { $sceneRepairRemoved++ }
}
$sceneRepairResult = $sceneRepairBuilder.ToString()
if ($sceneRepairRemoved -ne 10) { throw 'Unexpected removal count.' }
$sceneRepairResultIds = [regex]::Matches($sceneRepairResult, '(?m)^--- !u!\d+ &(\d+)')
if (@($sceneRepairResultIds | Group-Object { $_.Groups[1].Value } | Where-Object Count -gt 1).Count -gt 0) { throw 'Duplicates remain.' }
# A mechanical deduplication only: preserve the first object and every existing reference.
[System.IO.File]::WriteAllText($sceneRepairPath, $sceneRepairResult, [System.Text.UTF8Encoding]::new($false))
[PSCustomObject]@{ removedDuplicateBlocks = $sceneRepairRemoved; uniqueIds = $sceneRepairResultIds.Count; backup = $sceneRepairBackup }
