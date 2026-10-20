param([switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$generatedRoot=[IO.Path]::GetFullPath((Join-Path $workspace '.codex/opening-optimization-20261012'))
$manifestRoot=Join-Path $workspace '.codex/project-cleanup-20261018'
if(-not $generatedRoot.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Generated root is outside the workspace'}
$plans=@()
foreach($directory in Get-ChildItem -LiteralPath $generatedRoot -Directory){
    if($directory.Name -notmatch '^standalone-v[0-9]{3}$' -or $directory.Name -eq 'standalone-v165'){continue}
    $target=[IO.Path]::GetFullPath($directory.FullName)
    if(-not $target.StartsWith($generatedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw "Unsafe target: $target"}
    if($directory.Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Linked target: $target"}
    $revision=$directory.Name.Substring('standalone-'.Length)
    $reportPath=Join-Path $generatedRoot "build-report-$revision.json"
    if(-not(Test-Path -LiteralPath $reportPath)){continue}
    $report=Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if($report.result -ne 'Succeeded'){continue}
    $executable=Join-Path $target 'DarkBrineOpening.exe'
    if(-not(Test-Path -LiteralPath $executable)){continue}
    $expected=[IO.Path]::GetFullPath($report.path)
    if($expected -ne [IO.Path]::GetFullPath($executable)){throw "Report path mismatch: $target"}
    $entries=@(Get-ChildItem -LiteralPath $target -Recurse -Force)
    if($entries | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}){throw "Linked child in $target"}
    $files=@($entries | Where-Object {-not $_.PSIsContainer})
    if($files | Where-Object { $_.Extension -In '.cs','.blend','.fbx','.unity','.prefab','.mp4','.png' -or ($_.Extension -eq '.json' -and $_.Name -notin 'RuntimeInitializeOnLoads.json','ScriptingAssemblies.json') }){throw "Source or review evidence found in $target; leave it intact"}
    $plans+= [pscustomobject]@{path=$target;revision=$revision;bytes=($files | Measure-Object Length -Sum).Sum;executableSHA256=(Get-FileHash -LiteralPath $executable).Hash;files=@($files | ForEach-Object {[pscustomobject]@{path=$_.FullName.Substring($target.Length+1);bytes=$_.Length}});report=$report}
}
New-Item -ItemType Directory -Path $manifestRoot -Force | Out-Null
$plans | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $manifestRoot 'generated-build-cleanup-plan.json') -Encoding utf8
if($Apply){
    foreach($plan in $plans){
        # Re-resolve and recheck immediately before each recursive removal.
        $resolved=(Resolve-Path -LiteralPath $plan.path).ProviderPath
        if($resolved -ne $plan.path -or -not $resolved.StartsWith($generatedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw "Unsafe resolved target: $resolved"}
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    $plans | Select-Object path,revision,bytes,executableSHA256 | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $manifestRoot 'generated-build-cleanup-completed.json') -Encoding utf8
}
[pscustomobject]@{apply=[bool]$Apply;builds=$plans.Count;bytes=($plans | Measure-Object bytes -Sum).Sum;retained='standalone-v165, all source/recordings/reports, current Downloads build'} | ConvertTo-Json
