param([Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$Run,[ValidateRange(3,30)][int]$Seconds=6,[switch]$Final)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'RestoreBaseline019Prefs.ps1')
$qa024Root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance024'))
$qa024RunRoot=Join-Path $qa024Root ('runs/'+$Run)
if(Test-Path -LiteralPath $qa024RunRoot){throw 'Preserve existing run'}
New-Item -ItemType Directory -Path $qa024RunRoot -Force|Out-Null
$qa024BuildRoot=if($Final){Join-Path $qa024Root 'final-build'}else{$qa024Root}
$qa024Build=Get-Content -LiteralPath (Join-Path $qa024BuildRoot 'build-report.json') -Raw | ConvertFrom-Json
if($qa024Build.result -ne 'Succeeded'){throw 'Require successful current build'}
if(Get-Process | Where-Object ProcessName -Like 'Dark*'){throw 'Another Dark* Player is active; preserve it'}
$qa024Before=Join-Path $qa024RunRoot 'native-prefs-launch.clixml'
if(Test-Path -LiteralPath $qa024Before){throw 'Preserve previous launch evidence'}
$qa024RegPath='Software\DefaultCompany\Dark Brine_Rot'
$qa024Key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($qa024RegPath)
$qa024Exists=$null -ne $qa024Key
$qa024Values=@()
if($qa024Key){
    foreach($qa024Name in $qa024Key.GetValueNames()){
        $qa024Values += [pscustomobject]@{Name=$qa024Name;Kind=$qa024Key.GetValueKind($qa024Name).ToString();Value=$qa024Key.GetValue($qa024Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)}
    }
    $qa024Key.Close()
}
[pscustomobject]@{Path=$qa024RegPath;Exists=$qa024Exists;Values=$qa024Values}|Export-Clixml -LiteralPath $qa024Before
$qa024SavePath='C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot'
$qa024SaveHashes=@{}
foreach($qa024Name in @('dark-brine-rot-save.json','dark-brine-rot-save.json.bak')){
    $qa024File=Join-Path $qa024SavePath $qa024Name
    $qa024SaveHashes[$qa024Name]=if(Test-Path -LiteralPath $qa024File){(Get-FileHash -LiteralPath $qa024File -Algorithm SHA256).Hash}else{$null}
}
$qa024Output=Join-Path $qa024RunRoot 'player-report'
New-Item -ItemType Directory -Path $qa024Output -Force|Out-Null
$qa024Process=$null
$qa024TimedOut=$false
$qa024Exit=$null
$qa024PrefsRestored=$false
$qa024Failure=$null
try{
    [pscustomobject]@{forestAttribution=$true;native1080=$true;timingBuild=$true}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa024RunRoot 'condition.json')
    $qa024Exe=Join-Path $qa024BuildRoot 'player/DarkBrineQA024.exe'
    $qa024Args=@('-sahur-baseline',('-baseline-seconds='+$Seconds),'-baseline-native1080','-baseline-forest-attribution',('-baseline-report="'+(Join-Path $qa024Output 'report.json')+'"'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $qa024Output 'player.log')+'"'))
    $qa024Process=Start-Process -FilePath $qa024Exe -ArgumentList $qa024Args -WindowStyle Hidden -PassThru
    $null=$qa024Process.Handle
    [pscustomobject]@{pid=$qa024Process.Id;exe=$qa024Exe;startedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa024RunRoot 'owned-player.json')
    $qa024Deadline=[DateTime]::UtcNow.AddSeconds(240)
    while(!$qa024Process.HasExited -and [DateTime]::UtcNow -lt $qa024Deadline){Start-Sleep -Seconds 2;$qa024Process.Refresh()}
    if(!$qa024Process.HasExited){$qa024TimedOut=$true;Stop-Process -Id $qa024Process.Id; $qa024Process.WaitForExit()}
    $qa024Process.WaitForExit(); $qa024Exit=$qa024Process.ExitCode
}catch{$qa024Failure=$_.Exception.Message}
finally{
    if($qa024Process -and !$qa024Process.HasExited){Stop-Process -Id $qa024Process.Id; $qa024Process.WaitForExit()}
    if(!(Get-Process|Where-Object ProcessName -Like 'Dark*')){
        $qa024PrefsRestored=Restore-Qa019NativePreferences -SnapshotPath $qa024Before
    }
    $qa024SaveProof=@()
    foreach($qa024Name in $qa024SaveHashes.Keys){
        $qa024File=Join-Path $qa024SavePath $qa024Name
        $qa024ActualHash=if(Test-Path -LiteralPath $qa024File){(Get-FileHash -LiteralPath $qa024File -Algorithm SHA256).Hash}else{$null}
        $qa024SaveProof += [pscustomobject]@{file=$qa024Name;unchanged=($qa024ActualHash -eq $qa024SaveHashes[$qa024Name])}
    }
    [pscustomobject]@{timedOut=$qa024TimedOut;exitCode=$qa024Exit;failure=$qa024Failure;nativePreferencesRestored=$qa024PrefsRestored;saves=$qa024SaveProof}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $qa024RunRoot 'player-safety.json')
}
Get-Content -LiteralPath (Join-Path $qa024RunRoot 'player-safety.json')
