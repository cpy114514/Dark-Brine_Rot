param(
    [Parameter(Mandatory)] [string] $RuntimeDirectory,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [ValidateSet('Cappuccino','Flock','Nailong','Bombardino')] [string] $Encounter = 'Cappuccino',
    [ValidateRange(1, 120)] [int] $MaxActions = 70,
    [switch] $RetryFromDeathScreen
)

$ErrorActionPreference = 'Stop'
$stateFile = Join-Path $OutputDirectory 'state.json'
$logFile = Join-Path $OutputDirectory ($Encounter.ToLowerInvariant() + '-input.jsonl')

function Invoke-Player([string] $Command, [string[]] $Options) {
    $reply = & unity command --runtime-path $RuntimeDirectory --timeout 12 $Command @Options --json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or !$reply.success -or $reply.data.result.success -eq $false) {
        throw "Player command $Command failed."
    }
    return $reply.data.result
}

function Input-Key([string] $Key, [string] $Action) {
    $null = Invoke-Player 'simulate_key' @('--key', $Key, '--action', $Action)
}

function Input-Mouse([string] $Button, [string] $Action, [int] $X = 960, [int] $Y = 540) {
    $null = Invoke-Player 'simulate_pointer' @('--x', "$X", '--y', "$Y", '--button', $Button, '--action', $Action)
}

function Tap-Key([string] $Key) {
    Input-Key $Key 'down'
    try { Start-Sleep -Milliseconds 65 }
    finally { Input-Key $Key 'up' }
}

function Tap-Mouse([string] $Button, [int] $Hold = 65, [int] $X = 960, [int] $Y = 540) {
    # Queue events for the normal input update. This avoids a second
    # InputSystem.Update between a mouse edge and the gameplay Update.
    $mask = switch ($Button) { 'left' { 1 } 'right' { 2 } 'middle' { 4 } default { throw 'Mouse button' } }
    $code = @"
if (UnityEngine.Application.productName != "DarkBrineRot_Playtest") throw new System.InvalidOperationException();
var input = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
var mouse = mouseType.GetProperty("current").GetValue(null);
var stateType = System.Type.GetType("UnityEngine.InputSystem.LowLevel.MouseState, Unity.InputSystem");
var queue = input.GetMethods().Single(m => m.Name == "QueueStateEvent" && m.IsGenericMethod).MakeGenericMethod(stateType);
var host = UnityEngine.Object.FindFirstObjectByType(System.Type.GetType("Mavis.IslandPerformanceBaseline, Assembly-CSharp")) as UnityEngine.MonoBehaviour;
void Queue(ushort buttons)
{
    var state = System.Activator.CreateInstance(stateType);
    stateType.GetField("buttons").SetValue(state, buttons);
    stateType.GetField("position").SetValue(state, new UnityEngine.Vector2(${X}f, ${Y}f));
    queue.Invoke(null, new object[] { mouse, state, -1d });
}
System.Collections.IEnumerator Click()
{
    Queue($mask);
    yield return new UnityEngine.WaitForSecondsRealtime(${Hold}f / 1000f);
    Queue(0);
}
host.StartCoroutine(Click());
return true;
"@
    $null = Invoke-Player 'eval' @($code)
    Start-Sleep -Milliseconds ($Hold + 90)
}

$inspect = @'
if (UnityEngine.Application.productName != "DarkBrineRot_Playtest") throw new System.InvalidOperationException();
const string encounter = "__ENCOUNTER__";
var playerType = System.Type.GetType("ThirdPersonPlayerController, Assembly-CSharp");
var player = (UnityEngine.Component)UnityEngine.Object.FindFirstObjectByType(playerType);
var healthType = System.Type.GetType(encounter == "Nailong" ? "Mavis.NailongHealth, Assembly-CSharp" : "Mavis.Health, Assembly-CSharp");
var playerHealthType = System.Type.GetType("Mavis.PlayerHealth, Assembly-CSharp");
var staminaType = System.Type.GetType("Mavis.PlayerStamina, Assembly-CSharp");
var lockType = System.Type.GetType("Mavis.EnemyLockOn, Assembly-CSharp");
var lockOn = player.GetComponent(lockType);
var lockTarget = lockType.GetProperty("Target").GetValue(lockOn) as UnityEngine.Transform;
var playerHp = (float)playerHealthType.GetField("currentHealth").GetValue(player.GetComponent(playerHealthType));
var bossType = System.Type.GetType("Mavis." + (encounter == "Flock" ? "ChickenBroAI" : encounter == "Bombardino" ? "BombardinoBoss" : encounter + "AI") + ", Assembly-CSharp");
var enemies = UnityEngine.Object.FindObjectsByType(bossType,UnityEngine.FindObjectsSortMode.None).Cast<UnityEngine.Component>();
var healthField = healthType.GetField("currentHealth");
var boss = enemies.Where(e => e.GetComponent(healthType) && (float)healthField.GetValue(e.GetComponent(healthType)) > 0)
    .OrderBy(e => e.transform == lockTarget ? -1 : (e.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
if (!boss)
{
    var flockType=System.Type.GetType("Mavis.ChickenBroEncounter, Assembly-CSharp");
    var flock=UnityEngine.Object.FindFirstObjectByType(flockType);
    bool waiting=encounter == "Flock" && flock && !(bool)flockType.GetProperty("Completed").GetValue(flock);
    return new { alive = false, waiting, playerHp };
}
var bossHealth = boss.GetComponent(healthType);
var target = boss.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
var targetPoint = target ? target.bounds.center : boss.transform.position + UnityEngine.Vector3.up * 2;
var camera = UnityEngine.Camera.main;
var screen = camera.WorldToViewportPoint(targetPoint);
var flat = UnityEngine.Vector3.ProjectOnPlane(boss.transform.position - player.transform.position, UnityEngine.Vector3.up);
var aim = UnityEngine.Quaternion.LookRotation(targetPoint - player.transform.position - UnityEngine.Vector3.up * 1.5f).eulerAngles;
return new {
    alive = true, bossHp = healthType.GetField("currentHealth").GetValue(bossHealth),
    playerHp,
    stamina = staminaType.GetProperty("Current").GetValue(player.GetComponent(staminaType)),
    stage = bossType.GetProperty("CurrentState")?.GetValue(boss)?.ToString() ?? "Flock",
    locked = lockTarget == boss.transform,
    flatDistance = flat.magnitude, yaw = aim.y, pitch = UnityEngine.Mathf.DeltaAngle(0, aim.x),
    onScreen = screen.z > 0 && screen.x > .08f && screen.x < .92f && screen.y > .03f && screen.y < .97f
};
'@
$inspect = $inspect.Replace('__ENCOUNTER__', $Encounter)

function Aim-AtBoss([double] $Yaw, [double] $Pitch) {
    $yawNumber = $Yaw.ToString('R', [Globalization.CultureInfo]::InvariantCulture)
    $pitchNumber = $Pitch.ToString('R', [Globalization.CultureInfo]::InvariantCulture)
    $code = @"
if (UnityEngine.Application.productName != "DarkBrineRot_Playtest") throw new System.InvalidOperationException();
var t=System.Type.GetType("ThirdPersonPlayerController, Assembly-CSharp");
var p=UnityEngine.Object.FindFirstObjectByType(t);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var yaw=(float)t.GetField("yaw",flags).GetValue(p);
var pitch=(float)t.GetField("pitch",flags).GetValue(p);
var sensitivity=(float)t.GetField("mouseSensitivity").GetValue(p);
var m=System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
var input=System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
var delta=m.GetProperty("delta").GetValue(m.GetProperty("current").GetValue(null));
var dx=UnityEngine.Mathf.DeltaAngle(yaw,${yawNumber}f)/sensitivity;
var dy=-UnityEngine.Mathf.DeltaAngle(pitch,${pitchNumber}f)/sensitivity;
input.GetMethods().Single(x=>x.Name=="QueueDeltaStateEvent").MakeGenericMethod(typeof(UnityEngine.Vector2)).Invoke(null,new object[]{delta,new UnityEngine.Vector2(dx,dy),-1d});
return true;
"@
    $null = Invoke-Player 'eval' @($code)
}

if ($RetryFromDeathScreen) { Tap-Mouse 'left' 100 960 410; Start-Sleep -Milliseconds 1000 }
$heals = 0
for ($i = 0; $i -lt $MaxActions; $i++) {
    $sample = (Invoke-Player 'eval' @($inspect)).result
    $sample | ConvertTo-Json -Compress | Add-Content -LiteralPath $logFile
    if ($sample.playerHp -le 0) { return $sample }
    if ($sample.waiting) { Start-Sleep -Milliseconds 650; continue }
    if (!$sample.alive -or $sample.bossHp -le 0) { return $sample }
    if ($i % 5 -eq 0) { Write-Output "Attempt $i · Sahur $($sample.playerHp) · $Encounter $($sample.bossHp)" }
    if ($sample.playerHp -lt 50 -and $heals -lt 3) { Tap-Key 'H'; $heals++ }
    if (!$sample.locked) {
        if (!$sample.onScreen) { Aim-AtBoss $sample.yaw ([Math]::Clamp($sample.pitch - 5, -18, 20)); Start-Sleep -Milliseconds 400 }
        Tap-Mouse 'middle'
    }
    if ($sample.flatDistance -gt 5.5 -and $Encounter -ne 'Bombardino') {
        Input-Key 'W' 'down'
        try { Start-Sleep -Milliseconds ([int][Math]::Clamp(($sample.flatDistance - 5) * 95, 130, 1500)) }
        finally { Input-Key 'W' 'up' }
    }
    if ($sample.stage -eq 'Windup' -and $sample.stamina -ge 14) { Tap-Key 'LeftCtrl' }
    if ($Encounter -eq 'Bombardino') { Tap-Mouse 'left' 900 }
    elseif ($sample.stamina -ge 32 -and $sample.stage -in @('Recover','Staggered','Orbit','Flock')) { Tap-Mouse 'right' 750 }
    else { Tap-Mouse 'left'; Start-Sleep -Milliseconds 200; Tap-Mouse 'left' }
    Start-Sleep -Milliseconds 160
}
Write-Output "$Encounter input attempt reached $MaxActions actions."
