param(
    [Parameter(Mandatory)] [string] $ObservationDirectory,
    [string] $PlayerName = 'DarkBrineRot_Playtest'
)

$ErrorActionPreference = 'Stop'
$statePath = Join-Path $ObservationDirectory 'state.json'
$inputLog = Join-Path $ObservationDirectory 'inputs.jsonl'

function Read-Observation {
    for ($attempt = 0; $attempt -lt 5; $attempt++) {
        try { return Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json }
        catch { Start-Sleep -Milliseconds 100 }
    }
    throw 'No current passive observation.'
}

function Send-Input([string] $Command, [string[]] $Options) {
    $result = & unity command --runtime $PlayerName --timeout 15 $Command @Options --json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or !$result.success) { throw "Input command failed: $Command" }
    $payload = $result.data.result
    if ($payload -is [string] -and $payload.StartsWith('{')) { $payload = $payload | ConvertFrom-Json }
    if ($payload.success -eq $false) { throw "Input command failed: $($payload.message)" }
    [ordered]@{utc = [datetime]::UtcNow.ToString('o'); command = $Command; args = $Options} |
        ConvertTo-Json -Compress | Add-Content -LiteralPath $inputLog
}

function Send-Key([string] $Key, [string] $Action) {
    Send-Input 'simulate_key' @('--key', $Key, '--action', $Action)
}

function Send-Button([string] $Button, [string] $Action) {
    $state = Read-Observation
    Send-Input 'simulate_pointer' @('--x', [string][int]($state.renderWidth / 2), '--y', [string][int]($state.renderHeight / 2), '--button', $Button, '--action', $Action)
}

function Hold-Keys([string[]] $Keys, [int] $Milliseconds) {
    $held = @()
    try {
        foreach ($key in $Keys) { Send-Key $key 'down'; $held += $key }
        Start-Sleep -Milliseconds $Milliseconds
    }
    finally { foreach ($key in $held) { Send-Key $key 'up' } }
}

function Hold-Button([string] $Button, [int] $Milliseconds) {
    Send-Button $Button 'down'
    try { Start-Sleep -Milliseconds $Milliseconds }
    finally { Send-Button $Button 'up' }
}

# These are the actual tutorial bindings. Stop when a stage fails to respond so
# a blocked gate can be inspected in place. No simulation state is changed here.
$deadline = [datetime]::UtcNow.AddMinutes(9)
$lastLesson = ''
$lastInput = [datetime]::MinValue
$tries = 0
while ([datetime]::UtcNow -lt $deadline) {
    $state = Read-Observation
    if ($state.scene -like 'Assets/Scenes/First Island/*') { return $state }
    if ([datetime]::UtcNow - [datetime]$state.utc -gt [timespan]::FromSeconds(8)) { throw 'The Player observation stopped advancing.' }
    if (!$state.focused) { throw 'Focus changed. Preserve the current tutorial state for the next input.' }
    if ($state.openingStage -ne 'Tutorial') { Start-Sleep -Milliseconds 300; continue }
    if ($state.lesson -ne $lastLesson) { $lastLesson = $state.lesson; $tries = 0; $lastInput = [datetime]::MinValue }
    if ([datetime]::UtcNow - $lastInput -lt [timespan]::FromSeconds(2)) { Start-Sleep -Milliseconds 250; continue }
    if (!$state.tutorialWaiting -and $state.lesson -notin @('Move','Sprint','Swim')) { Start-Sleep -Milliseconds 250; continue }
    if (++$tries -gt 3) { throw "Opening input did not complete lesson $lastLesson after three attempts." }
    switch ($state.lesson) {
        'Move' { Hold-Keys @($(if ($tries -eq 2) { 'A' } elseif ($tries -eq 3) { 'D' } else { 'W' })) 2000 }
        'Sprint' { Hold-Keys @('LeftShift', $(if ($tries -eq 2) { 'S' } elseif ($tries -eq 3) { 'A' } else { 'W' })) 2300 }
        'Attack' { Hold-Button 'left' 120 }
        'Dodge' { Hold-Keys @('LeftCtrl') 120 }
        'ChargeHold' { Hold-Button 'right' 1800 }
        'ChargeRelease' { Send-Button 'right' 'up' }
        'Special' { Hold-Keys @('E') 120 }
        'Jump' { Hold-Keys @('Space') 120 }
        'Swim' { Hold-Keys @('W') 4300 }
        default { throw "Unexpected lesson $($state.lesson)" }
    }
    $lastInput = [datetime]::UtcNow
    Write-Output "Input sent for $lastLesson (attempt $tries)."
}
throw 'Opening playthrough exceeded its time budget.'
