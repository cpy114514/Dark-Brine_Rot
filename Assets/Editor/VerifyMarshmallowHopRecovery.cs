using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Walks off real moving platforms to verify course recovery.</summary>
public static class VerifyMarshmallowHopRecovery
{
    public static string Status = "idle", Result = "";
    static Keyboard keyboard;
    static Key[] pressed = Array.Empty<Key>();
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(pressed));
    }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds;
        var timeout = DateTime.UtcNow.AddSeconds(20);
        while (Time.time < end)
        {
            if (!EditorApplication.isPlaying || DateTime.UtcNow > timeout) throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(20);
        }
    }
    static async Task<bool> Until(Func<bool> check, float seconds)
    {
        float end = Time.time + seconds;
        while (!check() && Time.time < end) await Task.Delay(25);
        return check();
    }
    public static async void Run()
    {
        Status = "running"; Result = "";
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Enter play mode first.");
            var opening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if (opening) UnityEngine.Object.DestroyImmediate(opening.gameObject);
            var guide = UnityEngine.Object.FindFirstObjectByType<MarshmallowChallengeGuide>();
            if (guide) guide.enabled = false;
            var enemies = GameObject.Find("Marshmallow Friends - Second Island"); if (enemies) enemies.SetActive(false);
            var boss = UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>(); if (boss) boss.gameObject.SetActive(false);
            var root = GameObject.Find("Marshmallow Hop Challenge - 33");
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var recovery = root ? root.GetComponent<MarshmallowHopRecovery>() : null;
            if (!root || !player || !recovery) throw new Exception("Course, player or recovery system missing.");
            var body = player.GetComponent<CharacterController>();
            var hp = player.GetComponent<Mavis.PlayerHealth>();
            var stamina = player.GetComponent<Mavis.PlayerStamina>();
            var traversal = player.GetComponent<MarshmallowTraversal>();
            keyboard = Keyboard.current;
            if (!body || !hp || !stamina || !traversal || keyboard == null) throw new Exception("Player components missing.");
            var rows = new List<Transform>();
            foreach (Transform child in root.transform) if (child.name.StartsWith("Row ")) rows.Add(child);
            if (rows.Count != 22) throw new Exception("Expected 22 rows.");
            traversal.SetNormalGravity(false);
            hp.GrantProtection(120);
            InputSystem.onBeforeUpdate += Inject;

            async Task Place(int row)
            {
                pressed = Array.Empty<Key>();
                var box = rows[row - 1].GetChild(0).GetComponentInChildren<BoxCollider>();
                float offset = -(body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
                Vector3 at = box.bounds.center; at.y = box.bounds.max.y + offset + .06f;
                player.RestoreSavedPose(at, Quaternion.identity);
                typeof(ThirdPersonPlayerController).GetField("yaw", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 0f);
                stamina.currentStamina = stamina.maxStamina;
                if (!await Until(() => body.isGrounded && recovery.CurrentRow == row && recovery.RecoveryArmed, 4))
                    throw new Exception($"Row {row} was not tracked: row={recovery.CurrentRow}, grounded={body.isGrounded}, armed={recovery.RecoveryArmed}.");
                await Wait(.25f);
            }
            async Task Fall(int from, int expected)
            {
                if (recovery.CurrentRow != from || !recovery.RecoveryArmed)
                    throw new Exception($"Not armed at row {from}.");
                int count = recovery.RespawnCount;
                float beforeHp = hp.currentHealth;
                pressed = new[] { GameInputSettings.Get(GameInputSettings.Action.Left) };
                await Wait(.85f);
                pressed = Array.Empty<Key>();
                if (!await Until(() => recovery.RespawnCount > count, 8))
                    throw new Exception($"Falling from row {from} did not recover: feet={body.bounds.min.y:F1}, currentRow={recovery.CurrentRow}.");
                if (recovery.LastRespawnRow != expected)
                    throw new Exception($"Row {from} returned to {recovery.LastRespawnRow}, expected {expected}.");
                if (Mathf.Abs(hp.currentHealth - beforeHp) > .01f)
                    throw new Exception("Course recovery changed player health.");
                if (!await Until(() => body.isGrounded && recovery.CurrentRow == expected, 4))
                    throw new Exception($"Player did not land safely on row {expected}.");
                Result += $"{from}->{expected}; ";
                await Wait(.3f);
            }

            await Place(3); await Fall(3, 1);
            await Place(9); await Fall(9, 1);
            await Place(10); await Fall(10, 5);
            await Fall(5, 1);
            await Place(15); await Fall(15, 10);
            await Place(22); await Fall(22, 17);
            recovery.RestoreChallengeProgress(0, 0, 0, false, 0);
            await Place(1); await Fall(1, 1);
            await Place(9); await Fall(9, 1);
            await Place(9); await Fall(9, 1);
            await Place(9); await Fall(9, 7);
            if (!recovery.AssistanceActive || recovery.ConsecutiveFalls != 3 || recovery.RescueRow != 7)
                throw new Exception("Repeated falls did not activate the rescue checkpoint.");
            await Place(12);
            if (recovery.AssistanceActive || recovery.ConsecutiveFalls != 0)
                throw new Exception("Assistance did not finish after three rows of new progress.");
            Result += "safety net, fall rules, repeat-fall rescue and recovery after new progress passed";
            Status = "passed";
        }
        catch (Exception e) { Status = "failed"; Result += "FAILED: " + e; }
        finally
        {
            pressed = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject;
            if (keyboard != null) InputState.Change(keyboard, new KeyboardState());
        }
        File.WriteAllText("Temp/MarshmallowHopRecoveryReview.txt", Status + "\n" + Result);
        Debug.Log("Marshmallow hop recovery review: " + Status + " " + Result);
    }
}
