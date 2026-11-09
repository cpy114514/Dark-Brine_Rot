param(
    [ValidateSet('Preview','Apply')][string]$Mode='Preview',
    [string]$ReportDirectory
)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath (Split-Path $PSScriptRoot)).Path
if(-not $ReportDirectory){$ReportDirectory=Join-Path $taskWorkspace ('.codex/tool-cache-cleanup/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))}
$taskReport=[IO.Path]::GetFullPath($ReportDirectory)
if(Test-Path -LiteralPath $taskReport){throw 'Choose a new report directory; retain previous cleanup evidence.'}
$taskTracked=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$taskGitInfo=[Diagnostics.ProcessStartInfo]::new('git')
$taskGitInfo.WorkingDirectory=$taskWorkspace
$taskGitInfo.UseShellExecute=$false
$taskGitInfo.CreateNoWindow=$true
$taskGitInfo.RedirectStandardOutput=$true
$taskGitInfo.StandardOutputEncoding=[Text.Encoding]::UTF8
$taskGitInfo.ArgumentList.Add('ls-files')
$taskGitInfo.ArgumentList.Add('-z')
$taskGit=[Diagnostics.Process]::Start($taskGitInfo)
$taskTrackedOutput=$taskGit.StandardOutput.ReadToEnd()
$taskGit.WaitForExit()
if($taskGit.ExitCode -ne 0){$taskGit.Dispose();throw 'Cannot establish tracked files.'}
$taskGit.Dispose()
foreach($taskPath in $taskTrackedOutput.Split([char]0,[StringSplitOptions]::RemoveEmptyEntries)){$null=$taskTracked.Add($taskPath)}
$taskCandidates=@()
foreach($taskFolder in @('Tools','AgentScripts')){
    $taskRoot=Join-Path $taskWorkspace $taskFolder
    if(-not(Test-Path -LiteralPath $taskRoot)){continue}
    # Walk directories explicitly so links are rejected before descending into them.
    $taskQueue=[Collections.Generic.Queue[string]]::new()
    $taskQueue.Enqueue($taskRoot)
    while($taskQueue.Count){
        $taskCurrent=Get-Item -LiteralPath $taskQueue.Dequeue() -Force
        if($taskCurrent.Attributes -band [IO.FileAttributes]::ReparsePoint){continue}
        foreach($taskEntry in Get-ChildItem -LiteralPath $taskCurrent.FullName -Force){
            if($taskEntry.Attributes -band [IO.FileAttributes]::ReparsePoint){continue}
            if($taskEntry.PSIsContainer){$taskQueue.Enqueue($taskEntry.FullName);continue}
            if($taskEntry.Directory.Name -ne '__pycache__' -or $taskEntry.Name -notmatch '^(?<module>.+)\.(cpython|pypy)-[\w-]+(?:\.opt-\d+)?\.py[co]$'){continue}
            $taskSource=Join-Path $taskEntry.Directory.Parent.FullName ($Matches.module+'.py')
            if(-not(Test-Path -LiteralPath $taskSource -PathType Leaf)){continue}
            if((Get-Item -LiteralPath $taskSource).Attributes -band [IO.FileAttributes]::ReparsePoint){continue}
            $taskRelative=[IO.Path]::GetRelativePath($taskWorkspace,$taskEntry.FullName).Replace('\','/')
            if($taskTracked.Contains($taskRelative)){continue}
            $taskCandidates+=[pscustomobject][ordered]@{path=$taskRelative;source=[IO.Path]::GetRelativePath($taskWorkspace,$taskSource).Replace('\','/');bytes=$taskEntry.Length;sha256=(Get-FileHash -LiteralPath $taskEntry.FullName -Algorithm SHA256).Hash}
        }
    }
}
$null=New-Item -ItemType Directory -Path $taskReport
ConvertTo-Json -InputObject $taskCandidates -Depth 4|Set-Content -LiteralPath (Join-Path $taskReport 'manifest.json')
if($Mode -eq 'Apply'){
    # Validate all final absolute paths and current bytes before any removal.
    foreach($taskItem in $taskCandidates){
        $taskTarget=(Resolve-Path -LiteralPath (Join-Path $taskWorkspace $taskItem.path)).Path
        if(-not($taskTarget.StartsWith((Join-Path $taskWorkspace 'Tools')+'\',[StringComparison]::OrdinalIgnoreCase) -or $taskTarget.StartsWith((Join-Path $taskWorkspace 'AgentScripts')+'\',[StringComparison]::OrdinalIgnoreCase))){throw 'Cache path escaped the allowed roots.'}
        $taskAncestor=Get-Item -LiteralPath $taskTarget -Force
        while($taskAncestor.FullName -ne $taskWorkspace){
            if($taskAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Path changed to a link.'}
            $taskAncestor=if($taskAncestor -is [IO.DirectoryInfo]){$taskAncestor.Parent}else{$taskAncestor.Directory}
            if($null -eq $taskAncestor){throw 'Path ancestry did not reach the workspace.'}
        }
        if((Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash -ne $taskItem.sha256){throw 'Cache changed since inventory.'}
        if(-not(Test-Path -LiteralPath (Join-Path $taskWorkspace $taskItem.source) -PathType Leaf)){throw 'Python source disappeared.'}
    }
    foreach($taskItem in $taskCandidates){Remove-Item -LiteralPath (Join-Path $taskWorkspace $taskItem.path) -Force}
}
$taskResult=[ordered]@{mode=$Mode;files=$taskCandidates.Count;bytes=[long](($taskCandidates|Measure-Object bytes -Sum).Sum);report=$taskReport;scope='Only untracked Python __pycache__ files with existing source under Tools/AgentScripts. No Unity cache, assets, saves, builds, recordings or history.'}
$taskResult|ConvertTo-Json|Tee-Object -FilePath (Join-Path $taskReport 'result.json')
