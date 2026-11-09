param(
    [Parameter(Mandatory)] [string] $RuntimeDirectory,
    [Parameter(Mandatory)] [double] $X,
    [Parameter(Mandatory)] [double] $Z,
    [double] $StopDistance = 2,
    [int] $MaxSteps = 8,
    [switch] $Sprint
)

$ErrorActionPreference = 'Stop'
function Eval-Player([string] $Code) {
    $reply = & unity command --runtime-path $RuntimeDirectory eval $Code --json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or !$reply.success -or !$reply.data.result.success) {
        throw "Input step failed: $($reply.data.result.diagnostics | ConvertTo-Json -Compress)"
    }
    return $reply.data.result.result
}

$culture = [Globalization.CultureInfo]::InvariantCulture
$xArg = $X.ToString('R', $culture)
$zArg = $Z.ToString('R', $culture)
$targetArg = $StopDistance.ToString('R', $culture)
$sprintArg = if ($Sprint) { 'true' } else { 'false' }

# Each step calculates an ordinary mouse turn and a timed W or Shift+W input.
# Only input events enter the game. Walls, swimming, attacks and locks still apply.
$code = @"
if (UnityEngine.Application.productName != "DarkBrineRot_Playtest") throw new System.InvalidOperationException();
var controller = System.Type.GetType("ThirdPersonPlayerController, Assembly-CSharp");
var player = UnityEngine.Object.FindFirstObjectByType(controller) as UnityEngine.MonoBehaviour;
if (!player || !player.enabled || !UnityEngine.Application.isFocused || UnityEngine.Time.timeScale <= 0)
    return new { status = "controls unavailable", distance = -1f, seconds = 0f };
var pos = player.transform.position;
var offset = new UnityEngine.Vector3(${xArg}f - pos.x, 0, ${zArg}f - pos.z);
var distance = offset.magnitude;
if (distance <= ${targetArg}f) return new { status = "arrived", distance, seconds = 0f };
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var yaw = (float)controller.GetField("yaw",flags).GetValue(player);
var pitch = (float)controller.GetField("pitch",flags).GetValue(player);
var sensitivity = (float)controller.GetField("mouseSensitivity").GetValue(player);
var expected = UnityEngine.Quaternion.LookRotation(offset).eulerAngles.y;
var input = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
var mouse = mouseType.GetProperty("current").GetValue(null);
var delta = mouseType.GetProperty("delta").GetValue(mouse);
var look = new UnityEngine.Vector2(UnityEngine.Mathf.DeltaAngle(yaw, expected) / sensitivity, (pitch - 14) / sensitivity);
input.GetMethods().Single(m => m.Name == "QueueDeltaStateEvent").MakeGenericMethod(typeof(UnityEngine.Vector2))
    .Invoke(null, new object[] { delta, look, -1d });
var keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
var keyboard = keyboardType.GetProperty("current").GetValue(null);
var key = System.Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
var keyboardState = System.Type.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState, Unity.InputSystem");
var ctor = keyboardState.GetConstructor(new[] { key.MakeArrayType() });
var queue = input.GetMethods().Single(m => m.Name == "QueueStateEvent" && m.IsGenericMethod).MakeGenericMethod(keyboardState);
bool sprint = ${sprintArg} && distance > 15;
float speed = (float)controller.GetField("moveSpeed").GetValue(player);
if (sprint) speed *= (float)controller.GetField("sprintMultiplier").GetValue(player);
float seconds = UnityEngine.Mathf.Clamp((distance - ${targetArg}f * .5f) / speed, .08f, 6f);
void Keys(bool held)
{
    var keys = System.Array.CreateInstance(key, held ? (sprint ? 2 : 1) : 0);
    if (held) keys.SetValue(System.Enum.Parse(key, "W"), 0);
    if (held && sprint) keys.SetValue(System.Enum.Parse(key, "LeftShift"), 1);
    queue.Invoke(null, new object[] { keyboard, ctor.Invoke(new object[] { keys }), -1d });
}
System.Collections.IEnumerator Walk()
{
    yield return new UnityEngine.WaitForSecondsRealtime(.25f);
    Keys(true);
    yield return new UnityEngine.WaitForSecondsRealtime(seconds);
    Keys(false);
}
player.StartCoroutine(Walk());
return new { status = "moving", distance, seconds, position = pos.ToString() };
"@

for ($step = 0; $step -lt $MaxSteps; $step++) {
    $sample = Eval-Player $code
    $sample | ConvertTo-Json -Compress
    if ($sample.status -ne 'moving') { return }
    Start-Sleep -Milliseconds ([int]($sample.seconds * 1000 + 900))
}
