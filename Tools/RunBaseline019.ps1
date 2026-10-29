$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'RestoreBaseline019Prefs.ps1')
$qa019Root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance019'))
$qa019Build=Get-Content -LiteralPath (Join-Path $qa019Root 'build-report.json') -Raw | ConvertFrom-Json
if($qa019Build.result -ne 'Succeeded'){throw 'Require successful current build'}
if(Get-Process | Where-Object ProcessName -Like 'Dark*'){throw 'Another Dark* Player is active; preserve it'}
$qa019Before=Join-Path $qa019Root 'native-prefs-launch.clixml'
if(Test-Path -LiteralPath $qa019Before){throw 'Preserve previous launch evidence'}
$qa019RegPath='Software\DefaultCompany\Dark Brine_Rot'
$qa019Key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($qa019RegPath)
$qa019Exists=$null -ne $qa019Key
$qa019Values=@()
if($qa019Key){
    foreach($qa019Name in $qa019Key.GetValueNames()){
        $qa019Values += [pscustomobject]@{Name=$qa019Name;Kind=$qa019Key.GetValueKind($qa019Name).ToString();Value=$qa019Key.GetValue($qa019Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)}
    }
    $qa019Key.Close()
}
[pscustomobject]@{Path=$qa019RegPath;Exists=$qa019Exists;Values=$qa019Values}|Export-Clixml -LiteralPath $qa019Before
$qa019SavePath='C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot'
$qa019SaveHashes=@{}
foreach($qa019Name in @('dark-brine-rot-save.json','dark-brine-rot-save.json.bak')){
    $qa019File=Join-Path $qa019SavePath $qa019Name
    $qa019SaveHashes[$qa019Name]=if(Test-Path -LiteralPath $qa019File){(Get-FileHash -LiteralPath $qa019File -Algorithm SHA256).Hash}else{$null}
}
$qa019Output=Join-Path $qa019Root 'player-report'
New-Item -ItemType Directory -Path $qa019Output -Force|Out-Null
$qa019Process=$null
$qa019TimedOut=$false
$qa019Exit=$null
$qa019PrefsRestored=$false
$qa019Failure=$null
try{
    $qa019Exe=Join-Path $qa019Root 'player/DarkBrineQA.exe'
    $qa019Args=@('-sahur-baseline','-baseline-seconds=10',('-baseline-report="'+(Join-Path $qa019Output 'report.json')+'"'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $qa019Output 'player.log')+'"'))
    $qa019Process=Start-Process -FilePath $qa019Exe -ArgumentList $qa019Args -WindowStyle Hidden -PassThru
    [pscustomobject]@{pid=$qa019Process.Id;exe=$qa019Exe;startedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa019Root 'owned-player.json')
    $qa019Deadline=[DateTime]::UtcNow.AddSeconds(260)
    while(!$qa019Process.HasExited -and [DateTime]::UtcNow -lt $qa019Deadline){Start-Sleep -Seconds 2;$qa019Process.Refresh()}
    if(!$qa019Process.HasExited){$qa019TimedOut=$true;Stop-Process -Id $qa019Process.Id; $qa019Process.WaitForExit()}
    $qa019Exit=$qa019Process.ExitCode
}catch{$qa019Failure=$_.Exception.Message}
finally{
    if($qa019Process -and !$qa019Process.HasExited){Stop-Process -Id $qa019Process.Id; $qa019Process.WaitForExit()}
    if(!(Get-Process|Where-Object ProcessName -Like 'Dark*')){
        $qa019PrefsRestored=Restore-Qa019NativePreferences -SnapshotPath $qa019Before
    }
    $qa019SaveProof=@()
    foreach($qa019Name in $qa019SaveHashes.Keys){
        $qa019File=Join-Path $qa019SavePath $qa019Name
        $qa019ActualHash=if(Test-Path -LiteralPath $qa019File){(Get-FileHash -LiteralPath $qa019File -Algorithm SHA256).Hash}else{$null}
        $qa019SaveProof += [pscustomobject]@{file=$qa019Name;unchanged=($qa019ActualHash -eq $qa019SaveHashes[$qa019Name])}
    }
    [pscustomobject]@{timedOut=$qa019TimedOut;exitCode=$qa019Exit;failure=$qa019Failure;nativePreferencesRestored=$qa019PrefsRestored;saves=$qa019SaveProof}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $qa019Root 'player-safety.json')
}
Get-Content -LiteralPath (Join-Path $qa019Root 'player-safety.json')
