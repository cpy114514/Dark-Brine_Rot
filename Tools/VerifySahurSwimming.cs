using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifySahurSwimming
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
    public static async Task<object> Run()
    {
        Require(Application.isPlaying, "Requires Play mode.");
        var p = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var weapon = p.GetComponent<SahurSwimmingWeapon>();
        var attack = p.GetComponent<Mavis.SahurAttack>();
        var stamina = p.GetComponent<Mavis.PlayerStamina>();
        var position = p.transform.position;
        var rotation = p.transform.rotation;
        var cursor = Cursor.lockState;
        float originalStamina = stamina.currentStamina;
        var grip = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab")
            .GetComponent<SahurSwimmingWeapon>().stick;
        Vector3 gripPosition = grip.localPosition;
        Quaternion gripRotation = grip.localRotation;
        var results = new List<string>();
        GameObject platform = null;
        try
        {
            Cursor.lockState = CursorLockMode.Locked;
            Keys();
            p.RestoreSavedPose(new Vector3(850f, 1f, 591f), Quaternion.identity);
            await Task.Delay(1000);
            Require(p.Swimming && weapon.IsStowed, "Deep water must swim and stow stick.");
            Require(!p.CanUseAirAttack && !p.CanUseGroundAttack && !attack.stickHitbox.enabled,
                "Water must block ground/air attacks and weapon damage.");
            results.Add("Deep water: swimming, back stow, attacks disabled.");

            stamina.currentStamina = 80f;
            typeof(Mavis.PlayerStamina).GetField("nextRegenerationTime", Private).SetValue(stamina, Time.time + 2f);
            // An unfocused Editor unlocks the real cursor, so drive the same
            // swimming movement method directly for deterministic input tests.
            var tick = typeof(ThirdPersonPlayerController).GetMethod("UpdateSwimming", Private);
            p.enabled = false;
            float elapsed = 0f;
            float until = Time.time + 1f;
            while (Time.time < until)
            {
                tick.Invoke(p, new object[] { Vector3.forward, Vector3.forward, true, false });
                elapsed += Time.deltaTime;
                await Task.Delay(16);
            }
            tick.Invoke(p, new object[] { Vector3.forward, Vector3.forward, true, false });
            elapsed += Time.deltaTime;
            float spent = 80f - stamina.currentStamina;
            Require(p.FastSwimming, "Shift + movement must accelerate swimming.");
            Require(Mathf.Abs(spent - elapsed * p.fastSwimDrainPerSecond) < 1.2f,
                "Unexpected fast swimming stamina drain: " + spent);
            results.Add("Fast swim drains approximately 3 stamina/second: " + spent.ToString("F2"));

            stamina.currentStamina = 0f;
            typeof(Mavis.PlayerStamina).GetField("nextRegenerationTime", Private).SetValue(stamina, Time.time + 2f);
            tick.Invoke(p, new object[] { Vector3.forward, Vector3.forward, true, false });
            Require(p.Swimming && !p.FastSwimming, "Exhaustion must keep normal swimming available.");
            Keys();
            await Task.Delay(100);
            stamina.currentStamina = 50f;
            typeof(Mavis.PlayerStamina).GetField("nextRegenerationTime", Private).SetValue(stamina, Time.time + 2f);
            tick.Invoke(p, new object[] { Vector3.zero, Vector3.zero, true, false });
            Require(!p.FastSwimming && Mathf.Abs(stamina.currentStamina - 50f) < 0.1f, "Idle Shift must not spend stamina.");
            results.Add("Exhaustion falls back to ordinary swim; idle Shift costs nothing.");
            Keys();
            p.enabled = true;

            typeof(Mavis.SahurAttack).GetField("charging", Private).SetValue(attack, true);
            typeof(Mavis.SahurAttack).GetField("attacking", Private).SetValue(attack, true);
            typeof(Mavis.SahurAttack).GetField("attackQueued", Private).SetValue(attack, true);
            typeof(Mavis.SahurAttack).GetField("airImpactArmed", Private).SetValue(attack, true);
            await Task.Delay(150);
            Require(!attack.IsCombatMotionActive && !attack.stickHitbox.enabled, "Swimming must cancel ongoing combat.");
            attack.TriggerAttack();
            Require(!attack.IsCombatMotionActive, "Swimming must reject new attacks.");
            results.Add("Queued charge/combo/landing strike cancelled; new attacks rejected.");

            var bed = UnityEngine.Object.FindFirstObjectByType<ProceduralSeabed>().GetComponent<MeshCollider>();
            float footOffset = (float)typeof(ThirdPersonPlayerController).GetMethod("GetLowestFootOffset", Private).Invoke(p, null);
            Require(bed.Raycast(new Ray(new Vector3(850f, 200f, 591f), Vector3.down), out var floorHit, 1000f), "Test needs seabed.");
            p.RestoreSavedPose(new Vector3(850f, floorHit.point.y - footOffset + 0.02f, 591f), Quaternion.identity);
            await Task.Delay(180);
            Require(p.Swimming, "A deep seabed must never be mistaken for dry land.");
            results.Add("Contact with deep seabed still enters swimming.");

            platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Swimming QA shallow floor";
            platform.transform.position = new Vector3(850f, -0.55f, 591f);
            platform.transform.localScale = new Vector3(20f, 0.2f, 20f);
            Physics.SyncTransforms();
            p.RestoreSavedPose(new Vector3(850f, -0.45f - footOffset + 0.04f, 591f), Quaternion.identity);
            await Task.Delay(650);
            Require(!p.Swimming && !weapon.IsStowed, "Shallow water must wade and return weapon to hand.");
            Require(Vector3.Distance(weapon.stick.localPosition, gripPosition) < 0.0001f &&
                Quaternion.Angle(weapon.stick.localRotation, gripRotation) < 0.01f, "Original grip must be restored exactly.");
            Require(p.GetComponent<CharacterController>().isGrounded, "Shallow water must use the actual floor.");
            results.Add("Shallow-water grounding and exact original weapon grip restored.");

            platform.transform.position = new Vector3(850f, -1.6f, 591f);
            Physics.SyncTransforms();
            p.RestoreSavedPose(new Vector3(850f, -1.5f - footOffset + 0.04f, 591f), Quaternion.identity);
            stamina.currentStamina = 80f;
            tick.Invoke(p, new object[] { Vector3.zero, Vector3.zero, false, true });
            float lift = (float)typeof(ThirdPersonPlayerController).GetField("verticalSpeed", Private).GetValue(p);
            Require(lift > 0f && !p.Swimming && Mathf.Abs(stamina.currentStamina - (80f - stamina.jumpCost)) < 0.1f,
                "Shore breach must lift, spend jump cost once, and defer swimming reentry.");
            results.Add("Space near the shore provides a stamina-gated exit jump.");

            platform.transform.position = new Vector3(850f, 2f, 591f);
            Physics.SyncTransforms();
            p.RestoreSavedPose(new Vector3(850f, 2.1f - footOffset + 0.04f, 591f), Quaternion.identity);
            await Task.Delay(500);
            Require(!p.Swimming && p.CanUseGroundAttack && !weapon.IsStowed, "Land must restore combat.");
            results.Add("Land: normal locomotion and ground combat available.");
            return results;
        }
        finally
        {
            p.enabled = true;
            Keys();
            if (platform != null) UnityEngine.Object.Destroy(platform);
            p.RestoreSavedPose(position, rotation);
            stamina.currentStamina = originalStamina;
            Cursor.lockState = cursor;
        }
    }
}
