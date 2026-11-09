param(
    [Parameter(Mandatory)] [string] $RuntimeDirectory,
    [Parameter(Mandatory)] [string] $ObservationDirectory,
    [Parameter(Mandatory)] [int] $PlayerProcessId,
    [ValidateRange(1, 3)] [int] $Cycles = 3
)

$ErrorActionPreference = 'Stop'
$statePath = Join-Path $ObservationDirectory 'state.json'
$fixture = Join-Path $ObservationDirectory 'cappuccino-fixture-save.json'
$save = Join-Path $env:USERPROFILE 'AppData/LocalLow/DefaultCompany/DarkBrineRot_Playtest/dark-brine-rot-save.json'
$samples = Join-Path $ObservationDirectory 'repeat-memory.jsonl'

function Invoke-Player([string] $Command, [string[]] $Options) {
    $reply = & unity command --runtime-path $RuntimeDirectory --timeout 20 $Command @Options --json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or !$reply.success -or $reply.data.result.success -eq $false) {
        throw "Player command $Command failed."
    }
    return $reply.data.result
}

function Wait-State([scriptblock] $Match, [int] $Seconds) {
    $deadline = [datetime]::UtcNow.AddSeconds($Seconds)
    while ([datetime]::UtcNow -lt $deadline) {
        if (!(Get-Process -Id $PlayerProcessId -ErrorAction SilentlyContinue)) { throw 'The Player exited.' }
        try { $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json }
        catch { Start-Sleep -Milliseconds 250; continue }
        if (& $Match $state) { return $state }
        Start-Sleep -Milliseconds 400
    }
    throw 'Timed out waiting for the expected Player state.'
}

function Capture-State([string] $Label, [int] $Cycle) {
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    $process = Get-Process -Id $PlayerProcessId
    $boss = (Invoke-Player 'eval' @($bossRead)).result
    $record = [ordered]@{utc = [datetime]::UtcNow.ToString('o'); cycle = $Cycle; label = $Label;
        privateMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 1);
        workingMB = [math]::Round($process.WorkingSet64 / 1MB, 1); state = $state; boss = $boss}
    $record | ConvertTo-Json -Compress -Depth 8 | Add-Content -LiteralPath $samples
    return $record
}

$identity = (Invoke-Player 'eval' @('return new { product=UnityEngine.Application.productName, path=UnityEngine.Application.persistentDataPath };')).result
if ($identity.product -ne 'DarkBrineRot_Playtest' -or [IO.Path]::GetFullPath($identity.path) -ne [IO.Path]::GetFullPath((Split-Path -Parent $save))) {
    throw 'This command requires the isolated playtest profile.'
}

$buttonPosition = @'
var type = System.Type.GetType("MainMenuController, Assembly-CSharp");
var menu = UnityEngine.Object.FindFirstObjectByType(type);
var button = (UnityEngine.Component)type.GetField("continueButton").GetValue(menu);
var rect = (UnityEngine.RectTransform)button.transform;
var pixel = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
return new { x = (int)pixel.x, y = (int)pixel.y };
'@

$bossRead = @'
var t = System.Type.GetType("Mavis.CappuccinoAI, Assembly-CSharp");
var boss = UnityEngine.Object.FindFirstObjectByType(t) as UnityEngine.Component;
if (!boss) return new { active = false };
var health = boss.GetComponent(System.Type.GetType("Mavis.Health, Assembly-CSharp"));
return new { active = t.GetProperty("FightActive").GetValue(boss),
    state = t.GetProperty("CurrentState").GetValue(boss).ToString(),
    hp = health.GetType().GetField("currentHealth").GetValue(health), position = boss.transform.position.ToString() };
'@

for ($cycle = 1; $cycle -le $Cycles; $cycle++) {
    $null = Wait-State { param($s) $s.phase -eq 'scene:MainMenu' -and $s.focused } 30
    Copy-Item -LiteralPath $fixture -Destination $save -Force
    $button = (Invoke-Player 'eval' @($buttonPosition)).result
    $click = @('--x', "$($button.x)", '--y', "$($button.y)", '--button', 'left', '--action')
    $null = Invoke-Player 'simulate_pointer' ($click + 'down')
    Start-Sleep -Milliseconds 120
    $null = Invoke-Player 'simulate_pointer' ($click + 'up')
    $null = Wait-State { param($s) $s.phase -like 'island:*' -and $s.armed -and !$s.controlLocked } 55
    Start-Sleep -Seconds 5
    $loaded = Capture-State 'five-seconds-after-load' $cycle
    Write-Output "Cycle $cycle loaded. Private memory $($loaded.privateMB) MiB."

    & "$PSScriptRoot/FirstIslandWalkTo.ps1" -RuntimeDirectory $RuntimeDirectory -X 252 -Z 443 -StopDistance 2.4 -Sprint
    $null = Wait-State { param($s) $s.phase -eq 'island:cappuccino' } 30
    & "$PSScriptRoot/FirstIslandCapriInput.ps1" -RuntimeDirectory $RuntimeDirectory -OutputDirectory $ObservationDirectory -MaxActions 4
    $null = Wait-State { param($s) $s.phase -like 'death:*' -and $s.health -eq 0 } 180
    $before = Capture-State 'natural-defeat' $cycle
    & "$PSScriptRoot/FirstIslandKeyInput.ps1" -RuntimeDirectory $RuntimeDirectory -Key Enter
    $null = Wait-State { param($s) $s.health -gt 0 -and !$s.controlLocked -and $s.timeScale -gt 0 } 8
    $after = Capture-State 'respawn' $cycle
    Start-Sleep -Seconds 9
    $settled = Capture-State 'nine-seconds-after-respawn' $cycle
    if ($after.state.checkpoint -ne 'Cappuccino approach' -or $after.state.health -ne 100 -or $after.boss.active -or
        $settled.boss.active -or $settled.boss.hp -ne 999) {
        throw "Cycle $cycle did not return the boss to its initial encounter state."
    }
    Write-Output "Cycle $cycle respawned. Boss $($before.boss.hp) → $($settled.boss.hp) HP. Private memory $($settled.privateMB) MiB."
    if ($cycle -eq $Cycles) { break }

    # The pause menu has Save & Quit but no in-process title return. Use a
    # targeted scene reload here to measure retained memory in the same process.
    $returnTitle = 'var t=System.Type.GetType("Mavis.SceneLoadingScreen, Assembly-CSharp");return t.GetMethod("Load",new[]{typeof(string)}).Invoke(null,new object[]{"Assets/Scenes/Main Menu/MainMenu.unity"});'
    $null = Invoke-Player 'eval' @($returnTitle)
}
