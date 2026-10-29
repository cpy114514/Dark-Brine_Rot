param([Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$Run,[switch]$Early,[switch]$NoSync,[switch]$Adaptive,[switch]$Fixed)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'RestoreBaseline019Prefs.ps1')
$qa023Root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance023'))
$qa023RunRoot=Join-Path $qa023Root ('runs/'+$Run)
if(Test-Path -LiteralPath $qa023RunRoot){throw 'Preserve existing run'}
New-Item -ItemType Directory -Path $qa023RunRoot -Force|Out-Null
$qa023BuildRoot=if($Adaptive){Join-Path $qa023Root 'adaptive-build'}else{$qa023Root}
$qa023Build=Get-Content -LiteralPath (Join-Path $qa023BuildRoot 'build-report.json') -Raw | ConvertFrom-Json
if($qa023Build.result -ne 'Succeeded'){throw 'Require successful current build'}
if(Get-Process | Where-Object ProcessName -Like 'Dark*'){throw 'Another Dark* Player is active; preserve it'}
$qa023Before=Join-Path $qa023RunRoot 'native-prefs-launch.clixml'
if(Test-Path -LiteralPath $qa023Before){throw 'Preserve previous launch evidence'}
$qa023RegPath='Software\DefaultCompany\Dark Brine_Rot'
$qa023Key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($qa023RegPath)
$qa023Exists=$null -ne $qa023Key
$qa023Values=@()
if($qa023Key){
    foreach($qa023Name in $qa023Key.GetValueNames()){
        $qa023Values += [pscustomobject]@{Name=$qa023Name;Kind=$qa023Key.GetValueKind($qa023Name).ToString();Value=$qa023Key.GetValue($qa023Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)}
    }
    $qa023Key.Close()
}
[pscustomobject]@{Path=$qa023RegPath;Exists=$qa023Exists;Values=$qa023Values}|Export-Clixml -LiteralPath $qa023Before
$qa023SavePath='C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot'
$qa023SaveHashes=@{}
foreach($qa023Name in @('dark-brine-rot-save.json','dark-brine-rot-save.json.bak')){
    $qa023File=Join-Path $qa023SavePath $qa023Name
    $qa023SaveHashes[$qa023Name]=if(Test-Path -LiteralPath $qa023File){(Get-FileHash -LiteralPath $qa023File -Algorithm SHA256).Hash}else{$null}
}
$qa023Output=Join-Path $qa023RunRoot 'player-report'
New-Item -ItemType Directory -Path $qa023Output -Force|Out-Null
$qa023Process=$null
$qa023TimedOut=$false
$qa023Exit=$null
$qa023PrefsRestored=$false
$qa023Failure=$null
try{
    if($NoSync){
        # Change only an observed, typed setting for this controlled launch;
        # the immutable full snapshot above is restored in finally.
        $qa023SyncValues=@($qa023Values|Where-Object {$_.Name.StartsWith('DarkBrine.Settings.VSync_h',[StringComparison]::Ordinal)})
        if($qa023SyncValues.Count -ne 1 -or $qa023SyncValues[0].Kind -ne 'DWord' -or $qa023SyncValues[0].Value -isnot [int]){throw 'No unique observed four-byte VSync setting; do not guess'}
        $qa023Writable=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($qa023RegPath,$true)
        try{$qa023Writable.SetValue($qa023SyncValues[0].Name,[int]0,[Microsoft.Win32.RegistryValueKind]::DWord)}finally{$qa023Writable.Close()}
    }
    [pscustomobject]@{earlyRender=[bool]$Early;temporaryVsyncOff=[bool]$NoSync;adaptiveBuild=[bool]$Adaptive;fixedBudget=[bool]$Fixed}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa023RunRoot 'condition.json')
    $qa023Exe=Join-Path $qa023BuildRoot 'player/DarkBrineQA023.exe'
    $qa023Args=@('-sahur-baseline','-baseline-seconds=3','-baseline-load-trace','-baseline-load-only',('-baseline-report="'+(Join-Path $qa023Output 'report.json')+'"'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $qa023Output 'player.log')+'"'))
    if($Early){$qa023Args += '-baseline-startup-render'}
    if($Fixed){$qa023Args += '-baseline-fixed-init-budget'}
    $qa023Process=Start-Process -FilePath $qa023Exe -ArgumentList $qa023Args -WindowStyle Hidden -PassThru
    $null=$qa023Process.Handle
    [pscustomobject]@{pid=$qa023Process.Id;exe=$qa023Exe;startedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa023RunRoot 'owned-player.json')
    $qa023Deadline=[DateTime]::UtcNow.AddSeconds(240)
    while(!$qa023Process.HasExited -and [DateTime]::UtcNow -lt $qa023Deadline){Start-Sleep -Seconds 2;$qa023Process.Refresh()}
    if(!$qa023Process.HasExited){$qa023TimedOut=$true;Stop-Process -Id $qa023Process.Id; $qa023Process.WaitForExit()}
    $qa023Process.WaitForExit(); $qa023Exit=$qa023Process.ExitCode
}catch{$qa023Failure=$_.Exception.Message}
finally{
    if($qa023Process -and !$qa023Process.HasExited){Stop-Process -Id $qa023Process.Id; $qa023Process.WaitForExit()}
    if(!(Get-Process|Where-Object ProcessName -Like 'Dark*')){
        $qa023PrefsRestored=Restore-Qa019NativePreferences -SnapshotPath $qa023Before
    }
    $qa023SaveProof=@()
    foreach($qa023Name in $qa023SaveHashes.Keys){
        $qa023File=Join-Path $qa023SavePath $qa023Name
        $qa023ActualHash=if(Test-Path -LiteralPath $qa023File){(Get-FileHash -LiteralPath $qa023File -Algorithm SHA256).Hash}else{$null}
        $qa023SaveProof += [pscustomobject]@{file=$qa023Name;unchanged=($qa023ActualHash -eq $qa023SaveHashes[$qa023Name])}
    }
    [pscustomobject]@{timedOut=$qa023TimedOut;exitCode=$qa023Exit;failure=$qa023Failure;nativePreferencesRestored=$qa023PrefsRestored;saves=$qa023SaveProof}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $qa023RunRoot 'player-safety.json')
}
Get-Content -LiteralPath (Join-Path $qa023RunRoot 'player-safety.json')
