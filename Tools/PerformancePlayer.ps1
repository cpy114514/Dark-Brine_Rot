. (Join-Path $PSScriptRoot 'RestoreBaseline019Prefs.ps1')

function Invoke-PerformancePlayer {
    param(
        [Parameter(Mandatory)][string]$BuildRoot,
        [Parameter(Mandatory)][string]$RunRoot,
        [Parameter(Mandatory)][string]$ExeName,
        [Parameter(Mandatory)][string[]]$PlayerArguments,
        [Parameter(Mandatory)][int]$TimeoutSeconds,
        [pscustomobject]$Condition,
        [switch]$NewRun,
        [switch]$NoSync
    )

    $ErrorActionPreference = 'Stop'
    $mutex = [Threading.Mutex]::new($false, 'Local\DarkBrinePerformancePlayer')
    $locked = $false
    try {
        try {
            $locked = $mutex.WaitOne(0)
        } catch [Threading.AbandonedMutexException] {
            $locked = $true
            throw 'The previous performance test stopped before releasing the launcher lock'
        }
        if (!$locked) { throw 'Another performance test is active' }

        $build = Get-Content -LiteralPath (Join-Path $BuildRoot 'build-report.json') -Raw | ConvertFrom-Json
        if ($build.result -ne 'Succeeded') { throw 'Require successful current build' }
        if (Get-Process | Where-Object ProcessName -Like 'Dark*') { throw 'Another Dark* Player is active; preserve it' }
        if ($NewRun) {
            if (Test-Path -LiteralPath $RunRoot) { throw 'Preserve existing run' }
            New-Item -ItemType Directory -Path $RunRoot | Out-Null
        }
        $snapshot = Join-Path $RunRoot 'native-prefs-launch.clixml'
        if (Test-Path -LiteralPath $snapshot) { throw 'Preserve previous launch evidence' }

        $registryPath = 'Software\DefaultCompany\Dark Brine_Rot'
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($registryPath)
        $values = @()
        if ($key) {
            try {
                $values = @(foreach ($name in $key.GetValueNames()) {
                    [pscustomobject]@{
                        Name = $name
                        Kind = $key.GetValueKind($name).ToString()
                        Value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                    }
                })
            } finally {
                $key.Close()
            }
        }
        [pscustomobject]@{ Path = $registryPath; Exists = ($null -ne $key); Values = $values } |
            Export-Clixml -LiteralPath $snapshot

        $saveDirectory = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/DefaultCompany/Dark Brine_Rot'
        $saveHashes = @{}
        foreach ($name in @('dark-brine-rot-save.json', 'dark-brine-rot-save.json.bak')) {
            $path = Join-Path $saveDirectory $name
            $saveHashes[$name] = if (Test-Path -LiteralPath $path) {
                (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
            } else { $null }
        }

        $output = Join-Path $RunRoot 'player-report'
        New-Item -ItemType Directory -Path $output -Force | Out-Null
        $process = $null
        $timedOut = $false
        $exitCode = $null
        $prefsRestored = $false
        $failure = $null
        try {
            if ($NoSync) {
                $vsync = @($values | Where-Object {
                    $_.Name.StartsWith('DarkBrine.Settings.VSync_h', [StringComparison]::Ordinal)
                })
                if ($vsync.Count -ne 1 -or $vsync[0].Kind -ne 'DWord' -or $vsync[0].Value -isnot [int]) {
                    throw 'No unique observed four-byte VSync setting; do not guess'
                }
                $writable = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($registryPath, $true)
                try {
                    $writable.SetValue($vsync[0].Name, [int]0, [Microsoft.Win32.RegistryValueKind]::DWord)
                } finally {
                    $writable.Close()
                }
            }
            if ($null -ne $Condition) {
                $Condition | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $RunRoot 'condition.json')
            }
            $exe = Join-Path $BuildRoot "player/$ExeName"
            $reportPath = Join-Path $output 'report.json'
            $logPath = Join-Path $output 'player.log'
            $arguments = @('-sahur-baseline') + $PlayerArguments + @(
                "-baseline-report=`"$reportPath`"", '-screen-fullscreen', '0',
                '-screen-width', '1920', '-screen-height', '1080', '-logFile', "`"$logPath`""
            )
            $process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
            $null = $process.Handle
            [pscustomobject]@{ pid = $process.Id; exe = $exe; startedUtc = [DateTime]::UtcNow } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $RunRoot 'owned-player.json')

            $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
            while (!$process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
                Start-Sleep -Seconds 2
                $process.Refresh()
            }
            if (!$process.HasExited) {
                $timedOut = $true
                $process.Kill()
            }
            $process.WaitForExit()
            $exitCode = $process.ExitCode
        } catch {
            $failure = $_.Exception.Message
        } finally {
            if ($process -and !$process.HasExited) {
                $process.Kill()
                $process.WaitForExit()
            }
            if (!(Get-Process | Where-Object ProcessName -Like 'Dark*')) {
                $prefsRestored = Restore-Qa019NativePreferences -SnapshotPath $snapshot
            }
            $saveProof = @(foreach ($name in $saveHashes.Keys) {
                $path = Join-Path $saveDirectory $name
                $hash = if (Test-Path -LiteralPath $path) {
                    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
                } else { $null }
                [pscustomobject]@{ file = $name; unchanged = ($hash -eq $saveHashes[$name]) }
            })
            [pscustomobject]@{
                timedOut = $timedOut
                exitCode = $exitCode
                failure = $failure
                nativePreferencesRestored = $prefsRestored
                saves = $saveProof
            } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $RunRoot 'player-safety.json')
        }
        Get-Content -LiteralPath (Join-Path $RunRoot 'player-safety.json')
    } finally {
        if ($locked) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
}
