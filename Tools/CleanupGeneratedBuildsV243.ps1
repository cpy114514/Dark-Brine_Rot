param([ValidateSet('Plan','Apply')][string]$Mode='Plan')
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath (Split-Path $PSScriptRoot)).Path
$taskRoot=(Resolve-Path -LiteralPath (Join-Path $taskWorkspace '.codex/opening-optimization-20261012')).Path
$taskEvidence=Join-Path $taskWorkspace '.codex/project-performance-20261023'
$taskPlanPath=Join-Path $taskEvidence 'generated-build-cleanup-plan.json'
$taskSpecs=@(@{name='standalone';report='build-report.json'},@{name='standalone-snapshot';report='snapshot-build-report.json'})
$taskPlans=@()
foreach($taskSpec in $taskSpecs){
    $taskTarget=(Resolve-Path -LiteralPath (Join-Path $taskRoot $taskSpec.name)).Path
    if((Split-Path $taskTarget) -ne $taskRoot -or $taskSpec.name -notin @('standalone','standalone-snapshot')){throw 'Unexpected deletion target'}
    $taskReport=Get-Content -LiteralPath (Join-Path $taskRoot $taskSpec.report) -Raw | ConvertFrom-Json
    if($taskReport.result -ne 'Succeeded' -or [IO.Path]::GetFullPath($taskReport.path) -ne (Join-Path $taskTarget 'DarkBrineOpening.exe')){throw 'Successful build report does not match target'}
    $taskEntries=@(Get-Item -LiteralPath $taskTarget)+@(Get-ChildItem -LiteralPath $taskTarget -Force -Recurse)
    if($taskEntries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }){throw 'Links are outside cleanup scope'}
    $taskFiles=@($taskEntries | Where-Object {-not $_.PSIsContainer})
    if($taskFiles | Where-Object {$_.Extension -in @('.cs','.unity','.prefab','.meta','.anim','.fbx','.blend','.save','.mp4','.mov','.jpg','.png')}){throw 'Source, save or review evidence found in generated output'}
    foreach($taskProcess in Get-Process -ErrorAction SilentlyContinue){
        try{$taskProcessPath=$taskProcess.Path}catch{$taskProcessPath=$null}
        if($taskProcessPath -and $taskProcessPath.StartsWith($taskTarget+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Generated output is running'}
    }
    $taskManifest=@(foreach($taskFile in $taskFiles){[ordered]@{path=$taskFile.FullName.Substring($taskTarget.Length+1);bytes=$taskFile.Length;sha256=(Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash}})
    $taskPlans+=[pscustomobject][ordered]@{target=$taskTarget;report=$taskSpec.report;files=$taskManifest;bytes=($taskFiles | Measure-Object Length -Sum).Sum}
}
if(-not (Test-Path -LiteralPath (Join-Path $taskRoot 'standalone-v165/DarkBrineOpening.exe'))){throw 'Retained newest historical build is missing'}
if($Mode -eq 'Plan'){
    if(Test-Path -LiteralPath $taskPlanPath){throw 'Retain previous plan'}
    [ordered]@{scope='Only two obsolete generated player outputs. Keep v165, all reports, sources, recordings, saves, caches and Downloads.';targets=$taskPlans} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskPlanPath
}else{
    $taskApproved=Get-Content -LiteralPath $taskPlanPath -Raw | ConvertFrom-Json
    if(($taskApproved.targets | ConvertTo-Json -Depth 8 -Compress) -ne ($taskPlans | ConvertTo-Json -Depth 8 -Compress)){throw 'Build files changed since inventory'}
    foreach($taskPlan in $taskPlans){
        $taskVerified=(Resolve-Path -LiteralPath $taskPlan.target).Path
        if((Split-Path $taskVerified) -ne $taskRoot -or (Split-Path $taskVerified -Leaf) -notin @('standalone','standalone-snapshot')){throw 'Final absolute path check failed'}
        Remove-Item -LiteralPath $taskVerified -Recurse -Force
    }
    [ordered]@{removed=@($taskPlans | ForEach-Object {$_.target});bytes=($taskPlans | Measure-Object bytes -Sum).Sum;retained='standalone-v165; reports, sources, review evidence, saves, compiler/shader caches; Downloads untouched'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskEvidence 'generated-build-cleanup-result.json')
}
$taskPlans | Select-Object target,bytes | ConvertTo-Json
