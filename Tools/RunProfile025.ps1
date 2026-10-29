param([Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$Run,[ValidateRange(3,30)][int]$Seconds=6,[switch]$Final)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'RestoreBaseline019Prefs.ps1')
$qa025Root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.codex/performance025'))
$qa025RunRoot=Join-Path $qa025Root ('runs/'+$Run)
if(Test-Path -LiteralPath $qa025RunRoot){throw 'Preserve existing run'}
New-Item -ItemType Directory -Path $qa025RunRoot -Force|Out-Null
$qa025BuildRoot=$qa025Root
$qa025Build=Get-Content -LiteralPath (Join-Path $qa025BuildRoot 'build-report.json') -Raw | ConvertFrom-Json
if($qa025Build.result -ne 'Succeeded'){throw 'Require successful current build'}
if(Get-Process | Where-Object ProcessName -Like 'Dark*'){throw 'Another Dark* Player is active; preserve it'}
$qa025Before=Join-Path $qa025RunRoot 'native-prefs-launch.clixml'
if(Test-Path -LiteralPath $qa025Before){throw 'Preserve previous launch evidence'}
$qa025RegPath='Software\DefaultCompany\Dark Brine_Rot'
$qa025Key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($qa025RegPath)
$qa025Exists=$null -ne $qa025Key
$qa025Values=@()
if($qa025Key){
    foreach($qa025Name in $qa025Key.GetValueNames()){
        $qa025Values += [pscustomobject]@{Name=$qa025Name;Kind=$qa025Key.GetValueKind($qa025Name).ToString();Value=$qa025Key.GetValue($qa025Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)}
    }
    $qa025Key.Close()
}
[pscustomobject]@{Path=$qa025RegPath;Exists=$qa025Exists;Values=$qa025Values}|Export-Clixml -LiteralPath $qa025Before
$qa025SavePath='C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot'
$qa025SaveHashes=@{}
foreach($qa025Name in @('dark-brine-rot-save.json','dark-brine-rot-save.json.bak')){
    $qa025File=Join-Path $qa025SavePath $qa025Name
    $qa025SaveHashes[$qa025Name]=if(Test-Path -LiteralPath $qa025File){(Get-FileHash -LiteralPath $qa025File -Algorithm SHA256).Hash}else{$null}
}
$qa025Output=Join-Path $qa025RunRoot 'player-report'
New-Item -ItemType Directory -Path $qa025Output -Force|Out-Null
$qa025Process=$null
$qa025TimedOut=$false
$qa025Exit=$null
$qa025PrefsRestored=$false
$qa025Failure=$null
try{
    [pscustomobject]@{forestAttribution=$true;native1080=$true;timingBuild=$true}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa025RunRoot 'condition.json')
    $qa025Exe=Join-Path $qa025BuildRoot 'player/DarkBrineQA025.exe'
    $qa025Args=@('-sahur-baseline',('-baseline-seconds='+$Seconds),'-baseline-native1080','-baseline-forest-attribution','-baseline-forest-profiler',('-baseline-report="'+(Join-Path $qa025Output 'report.json')+'"'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $qa025Output 'player.log')+'"'))
    $qa025Process=Start-Process -FilePath $qa025Exe -ArgumentList $qa025Args -WindowStyle Hidden -PassThru
    $null=$qa025Process.Handle
    [pscustomobject]@{pid=$qa025Process.Id;exe=$qa025Exe;startedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $qa025RunRoot 'owned-player.json')
    $qa025Deadline=[DateTime]::UtcNow.AddSeconds(240)
    while(!$qa025Process.HasExited -and [DateTime]::UtcNow -lt $qa025Deadline){Start-Sleep -Seconds 2;$qa025Process.Refresh()}
    if(!$qa025Process.HasExited){$qa025TimedOut=$true;Stop-Process -Id $qa025Process.Id; $qa025Process.WaitForExit()}
    $qa025Process.WaitForExit(); $qa025Exit=$qa025Process.ExitCode
}catch{$qa025Failure=$_.Exception.Message}
finally{
    if($qa025Process -and !$qa025Process.HasExited){Stop-Process -Id $qa025Process.Id; $qa025Process.WaitForExit()}
    if(!(Get-Process|Where-Object ProcessName -Like 'Dark*')){
        $qa025PrefsRestored=Restore-Qa019NativePreferences -SnapshotPath $qa025Before
    }
    $qa025SaveProof=@()
    foreach($qa025Name in $qa025SaveHashes.Keys){
        $qa025File=Join-Path $qa025SavePath $qa025Name
        $qa025ActualHash=if(Test-Path -LiteralPath $qa025File){(Get-FileHash -LiteralPath $qa025File -Algorithm SHA256).Hash}else{$null}
        $qa025SaveProof += [pscustomobject]@{file=$qa025Name;unchanged=($qa025ActualHash -eq $qa025SaveHashes[$qa025Name])}
    }
    [pscustomobject]@{timedOut=$qa025TimedOut;exitCode=$qa025Exit;failure=$qa025Failure;nativePreferencesRestored=$qa025PrefsRestored;saves=$qa025SaveProof}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $qa025RunRoot 'player-safety.json')
}
Get-Content -LiteralPath (Join-Path $qa025RunRoot 'player-safety.json')
