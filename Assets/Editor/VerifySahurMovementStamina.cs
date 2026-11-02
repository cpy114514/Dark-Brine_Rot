using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Exercises real movement input and stamina in the current play session.</summary>
public static class VerifySahurMovementStamina
{
    public static string Status = "idle", Result = "";
    static Keyboard keyboard;
    static Key[] keys = Array.Empty<Key>();
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float until = Time.time + seconds;
        var timeout = DateTime.UtcNow.AddSeconds(20);
        while (Time.time < until)
        {
            if (!EditorApplication.isPlaying || DateTime.UtcNow > timeout) throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(30);
        }
    }
    public static async void Run()
    {
        Status = "running"; Result = "";
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Start play mode first.");
            var awakening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if (awakening) UnityEngine.Object.DestroyImmediate(awakening.gameObject);
            var marshmallows = GameObject.Find("Marshmallow Friends - Second Island"); if (marshmallows) marshmallows.SetActive(false);
            var boss = UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>(); if (boss) boss.gameObject.SetActive(false);
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var stamina = player.GetComponent<Mavis.PlayerStamina>();
            var health = player.GetComponent<Mavis.PlayerHealth>();
            keyboard = Keyboard.current;
            if (!player || !stamina || !health || keyboard == null) throw new Exception("Player, stamina or keyboard missing.");
            health.GrantProtection(60);
            InputSystem.onBeforeUpdate += Inject;
            await Wait(.5f);
            stamina.currentStamina = stamina.maxStamina;
            Vector3 start = player.transform.position;
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) };
            await Wait(2.5f);
            float walk = stamina.currentStamina;
            float distance = Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude;
            if (distance < 1f || walk >= stamina.maxStamina - 1.2f)
                throw new Exception($"Noncombat walking did not drain: distance={distance:F2}, SP={walk:F2}");
            Result += $"walking moved {distance:F1} and used {stamina.maxStamina - walk:F1} SP; ";
            keys = Array.Empty<Key>();
            await Wait(1.1f);
            if (stamina.currentStamina <= walk + .5f) throw new Exception("SP did not recover after stopping.");
            Result += "stopped recovery OK; ";
            stamina.currentStamina = stamina.maxStamina;
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward), GameInputSettings.Get(GameInputSettings.Action.Sprint) };
            await Wait(1.5f);
            float sprint = stamina.currentStamina;
            if (!player.IsSprinting || sprint >= stamina.maxStamina - 3f)
                throw new Exception($"Noncombat sprint failed: sprinting={player.IsSprinting}, SP={sprint:F2}");
            Result += $"noncombat sprint used {stamina.maxStamina - sprint:F1} SP; ";
            keys = Array.Empty<Key>(); await Wait(1f);
            stamina.currentStamina = stamina.maxStamina;
            stamina.NotifyCombat();
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward), GameInputSettings.Get(GameInputSettings.Action.Sprint) };
            await Wait(1.5f);
            float combatSprint = stamina.currentStamina;
            if (!player.IsSprinting || Mathf.Abs(combatSprint - sprint) > 1.5f)
                throw new Exception($"Combat sprint cost differed: exploration={sprint:F2}, combat={combatSprint:F2}");
            Result += "combat sprint uses same movement cost; ";
            stamina.currentStamina = 0;
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) };
            start = player.transform.position;
            await Wait(1.3f);
            if (stamina.currentStamina > .1f || Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude < .5f)
                throw new Exception("Exhausted walking must continue but not regenerate SP.");
            keys = Array.Empty<Key>(); await Wait(1.4f);
            if (stamina.currentStamina < 1f) throw new Exception("SP did not recover after exhausted movement stopped.");
            Result += "exhaustion and recovery OK";
            Status = "passed";
        }
        catch (Exception e) { Status = "failed"; Result += e; }
        finally { keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject; }
        File.WriteAllText("Temp/SahurMovementStaminaReview.txt", Status + "\n" + Result);
        Debug.Log("Movement stamina review: " + Status + " " + Result);
    }
}
