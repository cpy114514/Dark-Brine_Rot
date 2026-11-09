// Drive a short burst through normal mouse events in the isolated test build.
if (UnityEngine.Application.productName != "DarkBrineRot_Playtest" || UnityEngine.Application.isEditor)
    throw new System.InvalidOperationException("Isolated test Player required.");

var playerType = System.Type.GetType("ThirdPersonPlayerController, Assembly-CSharp");
var player = UnityEngine.Object.FindFirstObjectByType(playerType) as UnityEngine.MonoBehaviour;
var bossType = System.Type.GetType("Mavis.BombardinoBoss, Assembly-CSharp");
var boss = UnityEngine.Object.FindFirstObjectByType(bossType) as UnityEngine.Component;
if (!player || !player.enabled || !boss || UnityEngine.Time.timeScale <= 0 || !UnityEngine.Application.isFocused)
    return new { status = "controls unavailable" };

var body = boss.GetComponent<UnityEngine.Collider>();
var host = UnityEngine.Object.FindFirstObjectByType(System.Type.GetType("Mavis.IslandPerformanceBaseline, Assembly-CSharp")) as UnityEngine.MonoBehaviour;
var input = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
var mouse = mouseType.GetProperty("current").GetValue(null);
var stateType = System.Type.GetType("UnityEngine.InputSystem.LowLevel.MouseState, Unity.InputSystem");
var queue = input.GetMethods().Single(m => m.Name == "QueueStateEvent" && m.IsGenericMethod).MakeGenericMethod(stateType);
var state = System.Activator.CreateInstance(stateType);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var mouseSensitivity = playerType.GetField("mouseSensitivity");
var yaw = playerType.GetField("yaw", flags);
var pitch = playerType.GetField("pitch", flags);
var rifleSensitivity = playerType.GetMethod("RifleAimLookScale", flags);
var center = new UnityEngine.Vector2(UnityEngine.Screen.width / 2f, UnityEngine.Screen.height / 2f);

void QueueMouse(ushort buttons, UnityEngine.Vector2 delta)
{
    stateType.GetField("buttons").SetValue(state, buttons);
    stateType.GetField("position").SetValue(state, center);
    stateType.GetField("delta").SetValue(state, delta);
    queue.Invoke(null, new object[] { mouse, state, -1d });
}

System.Collections.IEnumerator FireBurst()
{
    var previous = boss.transform.position;
    var previousAt = UnityEngine.Time.time;
    var deadline = UnityEngine.Time.realtimeSinceStartup + 2.5f;
    try
    {
        while (boss && player && player.enabled && UnityEngine.Application.isFocused && UnityEngine.Time.timeScale > 0 &&
               !(bool)bossType.GetProperty("IsDead").GetValue(boss) && UnityEngine.Time.realtimeSinceStartup < deadline)
        {
            var camera = UnityEngine.Camera.main;
            if (!camera) break;
            var current = boss.transform.position;
            var velocity = (current - previous) / UnityEngine.Mathf.Max(.02f, UnityEngine.Time.time - previousAt);
            var target = body ? body.bounds.center : current;
            var distance = UnityEngine.Vector3.Distance(target, camera.transform.position);
            target += UnityEngine.Vector3.ClampMagnitude(velocity, 55) * UnityEngine.Mathf.Clamp(distance / 205f, 0, .22f);
            var direction = UnityEngine.Quaternion.LookRotation(target - camera.transform.position).eulerAngles;
            float x = UnityEngine.Mathf.DeltaAngle((float)yaw.GetValue(player), direction.y);
            float y = UnityEngine.Mathf.DeltaAngle((float)pitch.GetValue(player), direction.x);
            float sensitivity = (float)mouseSensitivity.GetValue(player) * (float)rifleSensitivity.Invoke(player, null);
            var movement = new UnityEngine.Vector2(x, -y) * (.65f / sensitivity);
            QueueMouse((ushort)(UnityEngine.Mathf.Abs(x) < 12 && UnityEngine.Mathf.Abs(y) < 12 ? 3 : 2), movement);
            previous = current;
            previousAt = UnityEngine.Time.time;
            yield return new UnityEngine.WaitForSecondsRealtime(.095f);
        }
    }
    finally { QueueMouse(0, UnityEngine.Vector2.zero); }
}

host.StartCoroutine(FireBurst());
return new { status = "firing", seconds = 2.5 };
