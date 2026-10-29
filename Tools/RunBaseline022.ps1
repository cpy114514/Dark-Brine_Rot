$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'RestoreBaseline019Prefs.ps1')
$qa022Root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance022'))
$qa022Build=Get-Content -LiteralPath (Join-Path $qa022Root 'build-report.json') -Raw | ConvertFrom-Json
if($qa022Build.result -ne 'Succeeded'){throw 'Require successful current build'}
if(Get-Process | Where-Object ProcessName -Like 'Dark*'){throw 'Another Dark* Player is active; preserve it'}
$qa022Before=Join-Path $qa022Root 'native-prefs-launch.clixml'
if(Test-Path -LiteralPath $qa022Before){throw 'Preserve previous launch evidence'}
$qa022RegPath='Software\DefaultCompany\Dark Brine_Rot'
$qa022Key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($qa022RegPath)
$qa022Exists=$null -ne $qa022Key
$qa022Values=@()
if($qa022Key){
    foreach($qa022Name in $qa022Key.GetValueNames()){
        $qa022Values += [pscustomobject]@{Name=$qa022Name;Kind=$qa022Key.GetValueKind($qa022Name).ToString();Value=$qa022Key.GetValue($qa022Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)}
    }
    $qa022Key.Close()
}
[pscustomobject]@{Path=$qa022RegPath;Exists=$qa022Exists;Values=$qa022Values}|Export-Clixml -LiteralPath $qa022Before
$qa022SavePath='C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot'
$qa022SaveHashes=@{}
foreach($qa022Name in @('dark-brine-rot-save.json','dark-brine-rot-save.json.bak')){
    $qa022File=Join-Path $qa022SavePath $qa022Name
    $qa022SaveHashes[$qa022Name]=if(Test-Path -LiteralPath $qa022File){(Get-FileHash -LiteralPath $qa022File -Algorithm SHA256).Hash}else{$null}
}
$qa022Output=Join-Path $qa022Root 'player-report'
New-Item -ItemType Directory -Path $qa022Output -Force|Out-Null
$qa022Process=$null
$qa022TimedOut=$false
$qa022Exit=$null
$qa022PrefsRestored=$false
$qa022Failure=$null
try{
    $qa022Exe=Join-Path $qa022Root 'player/DarkBrineQA022.exe'
    $qa022Args=@('-sahur-baseline','-baseline-seconds=6','-baseline-native1080','-baseline-all-styles','-baseline-orbit',('-baseline-report="'+(Join-Path $qa022Output 'report.json')+'"'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $qa022Output 'player.log')+'"'))
    $qa022Process=Start-Process -FilePath $qa022Exe -ArgumentList $qa022Args -WindowStyle Hidden -PassThru
    $null=$qa022Process.Handle
    [pscustomobject]@{pid=$qa022Process.Id;exe=$qa022Exe;startedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa022Root 'owned-player.json')
    $qa022Deadline=[DateTime]::UtcNow.AddSeconds(420)
    while(!$qa022Process.HasExited -and [DateTime]::UtcNow -lt $qa022Deadline){Start-Sleep -Seconds 2;$qa022Process.Refresh()}
    if(!$qa022Process.HasExited){$qa022TimedOut=$true;Stop-Process -Id $qa022Process.Id; $qa022Process.WaitForExit()}
    $qa022Process.WaitForExit(); $qa022Exit=$qa022Process.ExitCode
}catch{$qa022Failure=$_.Exception.Message}
finally{
    if($qa022Process -and !$qa022Process.HasExited){Stop-Process -Id $qa022Process.Id; $qa022Process.WaitForExit()}
    if(!(Get-Process|Where-Object ProcessName -Like 'Dark*')){
        $qa022PrefsRestored=Restore-Qa019NativePreferences -SnapshotPath $qa022Before
    }
    $qa022SaveProof=@()
    foreach($qa022Name in $qa022SaveHashes.Keys){
        $qa022File=Join-Path $qa022SavePath $qa022Name
        $qa022ActualHash=if(Test-Path -LiteralPath $qa022File){(Get-FileHash -LiteralPath $qa022File -Algorithm SHA256).Hash}else{$null}
        $qa022SaveProof += [pscustomobject]@{file=$qa022Name;unchanged=($qa022ActualHash -eq $qa022SaveHashes[$qa022Name])}
    }
    [pscustomobject]@{timedOut=$qa022TimedOut;exitCode=$qa022Exit;failure=$qa022Failure;nativePreferencesRestored=$qa022PrefsRestored;saves=$qa022SaveProof}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $qa022Root 'player-safety.json')
}
Get-Content -LiteralPath (Join-Path $qa022Root 'player-safety.json')
