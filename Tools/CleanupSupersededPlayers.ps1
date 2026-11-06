param([ValidateSet('Plan','Apply')][string]$Mode='Plan')
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath (Split-Path $PSScriptRoot)).Path
$taskEvidence=Join-Path $taskWorkspace '.codex/project-maintenance-20261103'
$taskPlanPath=Join-Path $taskEvidence 'player-cleanup-plan.json'
# Explicit obsolete outputs only. Reports, captures, source snapshots and newer players stay.
$taskSpecs=@(
    @{folder='performance019/player';report='performance019/build-report.json'},
    @{folder='performance022/player';report='performance022/build-report.json'},
    @{folder='performance023/player';report='performance023/build-report.json'},
    @{folder='performance024/player';report='performance024/build-report.json'},
    @{folder='grass027/build/player';report='grass027/build/build-report.json'},
    @{folder='grass028/build/player';report='grass028/build/build-report.json'},
    @{folder='origami029/build/player';report='origami029/build/build-report.json'},
    @{folder='comic030/build/player';report='comic030/build/build-report.json'},
    @{folder='art031/build/player';report='art031/build/build-report.json'}
)
$taskGenerated=(Resolve-Path -LiteralPath (Join-Path $taskWorkspace '.codex')).Path
foreach($taskRetained in @('sync033/build/player/DarkBrineQA033.exe','grass032/build/player/DarkBrineQA032.exe',
    'performance023/adaptive-build/player/DarkBrineQA023.exe','performance024/final-build/player/DarkBrineQA024.exe')){
    if(-not(Test-Path -LiteralPath (Join-Path $taskGenerated $taskRetained))){throw "Retained build missing: $taskRetained"}
}
$taskProcessPaths=@(Get-Process -ErrorAction SilentlyContinue | ForEach-Object {try{$_.Path}catch{}})
$taskPlans=@(foreach($taskSpec in $taskSpecs){
    $taskExpected=[IO.Path]::GetFullPath((Join-Path $taskGenerated $taskSpec.folder))
    $taskTarget=(Resolve-Path -LiteralPath $taskExpected).Path
    if($taskTarget -ne $taskExpected -or -not $taskTarget.StartsWith($taskGenerated+'\',[StringComparison]::OrdinalIgnoreCase)){
        throw 'Target escaped explicit generated-output directory'
    }
    # Check every ancestor before recursive enumeration, then every descendant for links.
    $taskAncestor=Get-Item -LiteralPath $taskTarget
    while($taskAncestor.FullName -ne $taskWorkspace){
        if($taskAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked directory rejected'}
        $taskAncestor=$taskAncestor.Parent
    }
    $taskReport=Get-Content -LiteralPath (Join-Path $taskGenerated $taskSpec.report) -Raw | ConvertFrom-Json
    if($taskReport.result -ne 'Succeeded' -or (Split-Path ([IO.Path]::GetFullPath($taskReport.output))) -ne $taskTarget){
        throw 'Successful build report does not match target'
    }
    if(-not(Test-Path -LiteralPath $taskReport.output)){throw 'Built executable absent'}
    foreach($taskProcessPath in $taskProcessPaths){
        if($taskProcessPath -and $taskProcessPath.StartsWith($taskTarget+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Player is running'}
    }
    $taskEntries=@(Get-ChildItem -LiteralPath $taskTarget -Recurse -Force)
    if($taskEntries | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}){throw 'Linked output rejected'}
    $taskFiles=@($taskEntries | Where-Object {-not $_.PSIsContainer} | Sort-Object FullName)
    if($taskFiles | Where-Object {$_.Extension -in @('.cs','.unity','.prefab','.meta','.anim','.fbx','.blend','.save','.json','.mp4','.mov','.jpg','.png','.md','.txt') -and $_.Name -notin @('RuntimeInitializeOnLoads.json','ScriptingAssemblies.json','lib_burst_generated.txt')}){
        throw 'Source, save, report or review file found inside player output'
    }
    $taskFilesManifest=@(foreach($taskFile in $taskFiles){[ordered]@{
        path=$taskFile.FullName.Substring($taskTarget.Length+1);bytes=$taskFile.Length
        sha256=(Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash
    }})
    [pscustomobject][ordered]@{target=$taskTarget;report=$taskSpec.report;bytes=($taskFiles | Measure-Object Length -Sum).Sum;files=$taskFilesManifest}
})
New-Item -ItemType Directory -Force $taskEvidence | Out-Null
if($Mode -eq 'Plan'){
    if(Test-Path -LiteralPath $taskPlanPath){throw 'Existing plan must be retained'}
    $taskPlans | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskPlanPath
}else{
    $taskApproved=@(Get-Content -LiteralPath $taskPlanPath -Raw | ConvertFrom-Json)
    if(($taskApproved | ConvertTo-Json -Depth 8 -Compress) -ne ($taskPlans | ConvertTo-Json -Depth 8 -Compress)){throw 'Inventory changed; nothing removed'}
    foreach($taskPlan in $taskPlans){
        $taskVerified=(Resolve-Path -LiteralPath $taskPlan.target).Path
        if($taskVerified -ne $taskPlan.target -or -not $taskVerified.StartsWith($taskGenerated+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Final absolute target check failed'}
        Remove-Item -LiteralPath $taskVerified -Recurse -Force
    }
    [ordered]@{removed=@($taskPlans.target);files=($taskPlans.files | Measure-Object).Count;bytes=($taskPlans | Measure-Object bytes -Sum).Sum} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskEvidence 'player-cleanup-result.json')
}
$taskPlans | Select-Object target,bytes | ConvertTo-Json
