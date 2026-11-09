param(
    [Parameter(Mandatory)] [string] $RuntimeDirectory,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Za-z0-9]+$')] [string] $Key,
    [ValidateRange(30, 15000)] [int] $HoldMs = 100
)

$code = @"
if (UnityEngine.Application.productName != "DarkBrineRot_Playtest") throw new System.InvalidOperationException();
var input = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
var keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
var keyboard = keyboardType.GetProperty("current").GetValue(null);
var keyType = System.Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
var key = System.Enum.Parse(keyType, "$Key");
var stateType = System.Type.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState, Unity.InputSystem");
var ctor = stateType.GetConstructor(new[] { keyType.MakeArrayType() });
var queue = input.GetMethods().Single(m => m.Name == "QueueStateEvent" && m.IsGenericMethod).MakeGenericMethod(stateType);
var host = UnityEngine.Object.FindFirstObjectByType(System.Type.GetType("Mavis.IslandPerformanceBaseline, Assembly-CSharp")) as UnityEngine.MonoBehaviour;
void SetKey(bool held)
{
    var keys = System.Array.CreateInstance(keyType, held ? 1 : 0);
    if (held) keys.SetValue(key, 0);
    queue.Invoke(null, new object[] { keyboard, ctor.Invoke(new object[] { keys }), -1d });
}
System.Collections.IEnumerator Tap()
{
    SetKey(true);
    yield return new UnityEngine.WaitForSecondsRealtime(${HoldMs}f / 1000f);
    SetKey(false);
}
host.StartCoroutine(Tap());
return true;
"@

$reply = & unity command --runtime-path $RuntimeDirectory eval $code --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or !$reply.success -or !$reply.data.result.success) {
    throw "Buffered key $Key failed: $($reply.data.result.diagnostics | ConvertTo-Json -Compress)"
}
Start-Sleep -Milliseconds ($HoldMs + 120)
